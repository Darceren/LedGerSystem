# LedGerDB 记账规则说明

> 版本 1.0 | 业务日切：**自然日 0 点**（`TransDateOnly`）  
> 余额来源：**期初余额 OpeningBalance + 流水 LedgerTransaction**（Service 层计算，库内不冗余余额字段）

---

## 1. 表结构一览

| 表名 | 用途 |
|------|------|
| `Sys_User` | 登录用户（现阶段 shamim 单账号） |
| `Sys_TransactionType` | 流水类型字典 |
| `Party` | 参与方：客户、USDT 供应商、Shufen、其他（Final Account 可自由归类） |
| `BankAccount` | 本人银行账户（约 10 个，分户统计） |
| `OpeningBalance` | 上线日期初余额 |
| `LedgerTransaction` | 全部业务流水（含费用、调账） |

---

## 2. 枚举约定

### PartyType（参与方类型）

| 值 | 含义 |
|----|------|
| 1 | Customer 客户 |
| 2 | UsdtSupplier USDT 供应商 |
| 3 | Shufen |
| 4 | Other 其他（Final Account 自由维护） |

### FinalAccountSide（日终总账归属）

| 值 | 含义 | 对应手工账 |
|----|------|------------|
| 0 | 不显示在 Final Account | — |
| 1 | Credit 贷方（应收/资产） | 图 33 左侧 |
| 2 | Debit 借方（应付/负债） | 图 33 右侧 |

> 客户、现金、本人银行默认 **Credit**；USDT 供应商默认 **Debit**；Shufen 根据净额正负在报表中归入贷或借。

### PaymentMode（客户付 BDT 方式）

| 值 | 含义 |
|----|------|
| 1 | Cash 现金 |
| 2 | MyBank 本人银行 |
| 3 | SupplierBank 供应商银行（三角结算） |

### PayMethod（付 BDT 给供应商）

| 值 | 含义 |
|----|------|
| 1 | Cash |
| 2 | Bank |

### PayoutChannel（卖 RMB 出货渠道）

| 值 | 含义 |
|----|------|
| 1 | Alipay |
| 2 | WeChat |
| 3 | Bank |

### OpeningBalance.EntityType

| 值 | 含义 |
|----|------|
| 1 | Party |
| 2 | BankAccount |
| 3 | Cash 手头现金 |

---

## 3. 流水类型与必填字段

| Code | 名称 | 必填字段 |
|------|------|----------|
| `BUY_USDT` | 买 USDT | PartyId=供应商；Quantity+QuantityCurrency=USDT；Amount+Currency=BDT 实付；UnitPrice=BDT/USDT 成本价；PayMethod |
| `USDT_TO_RMB` | USDT→RMB | PartyId=Shufen；Quantity=USDT；UnitPrice=汇率；EquivalentAmount+EquivalentCurrency=RMB 收到 |
| `SELL_RMB` | 卖 RMB | PartyId=客户；Amount+Currency=CNY；UnitPrice=BDT/RMB 卖价；EquivalentAmount=BDT 应收；PayoutChannel；账号名/号可选 |
| `COLLECT_BDT_CASH` | 收 BDT 现金 | PartyId=客户；Amount=BDT；PaymentMode=1 |
| `COLLECT_BDT_BANK` | 收 BDT 到本人银行 | PartyId=客户；Amount=BDT；BankAccountId；PaymentMode=2 |
| `COLLECT_BDT_SUPPLIER` | 客户付供应商银行 | PartyId=客户；RelatedPartyId=供应商；Amount=BDT；PaymentMode=3 |
| `PAY_BDT_SUPPLIER` | 付 BDT 给供应商 | PartyId=供应商；Amount=BDT；PayMethod；BankAccountId（银行付款时） |
| `EXPENSE` | 费用 | Amount+BDT；Remark 自由填写 |
| `OPENING_BALANCE` | 期初（流水式） | 与 OpeningBalance 表二选一或同步；IsOpening=1 |
| `ADJUSTMENT` | 调账 | IsAdjustment=1；Remark 必填原因；建议关联 ReversedTransId |

---

## 4. 余额变动规则（Service 实现依据）

符号约定（**Party 余额**）：

- **客户**：正数 = 客户欠你 BDT（应收增加）；收款时 **减少**
- **供应商**：正数 = 你欠供应商 BDT（应付增加）；付款时 **减少**；客户代付供应商时 **减少**（三角结算）
- **Shufen**：正数 = 你欠 Shufen；负数 = Shufen 欠你（双向）

### 4.1 BUY_USDT

```
现金/银行 BDT 减少 Amount
供应商应付 BDT 增加 Amount（或 USDT 头寸增加，按 shamim 习惯以供应商往来为主）
USDT 数量记入 Quantity（用于成本核算）
```

### 4.2 USDT_TO_RMB

```
对 Shufen：USDT 侧减少 Quantity；RMB 侧增加 EquivalentAmount
净额更新 Shufen 往来（多付/少付形成双向余额）
```

### 4.3 SELL_RMB

```
客户应收 BDT 增加 EquivalentAmount（= RMB Amount × UnitPrice）
RMB 出货从「逻辑 RMB 池」减少 Amount（或通过 Shufen 往来体现）
```

对应 **图 11**：按 `PartyId + TransDateOnly` 汇总 `SELL_RMB` 的 CNY Amount。

### 4.4 COLLECT_BDT_CASH

```
手头现金 +Amount
客户应收 BDT -Amount
```

### 4.5 COLLECT_BDT_BANK

```
对应 BankAccount 余额 +Amount
客户应收 BDT -Amount
```

### 4.6 COLLECT_BDT_SUPPLIER（三角结算）

```
客户应收 BDT -Amount
供应商应付 BDT -Amount（客户代你付给供应商）
不增加本人现金/银行
```

### 4.7 PAY_BDT_SUPPLIER

```
PayMethod=Cash：手头现金 -Amount
PayMethod=Bank：对应 BankAccount -Amount
供应商应付 BDT -Amount
```

### 4.8 EXPENSE

```
费用从利润扣除；若从现金支付则手头现金 -Amount
若从银行支付则 BankAccount -Amount
```

### 4.9 OPENING_BALANCE / ADJUSTMENT

```
按 Remark 与实体类型调整对应 Party / Bank / Cash 期初或当期余额
调账优先冲正：新建 ADJUSTMENT 并填 ReversedTransId
```

---

## 5. 日终报表规则

### 5.1 客户每日 BDT 付款（图 22 下半部分）

```sql
-- 逻辑：TransDateOnly = @date AND TransType IN (COLLECT_BDT_*)
-- GROUP BY PartyId, PaymentMode
```

### 5.2 客户每日 RMB 出货（图 11）

```sql
-- TransType = SELL_RMB
-- GROUP BY PartyId, SUM(Amount) WHERE Currency='CNY'
```

### 5.3 Final Account（图 33）

**截止日 `@date`（含当日，`TransDateOnly <= @date`）：**

| 区块 | 数据来源 |
|------|----------|
| Credit 贷方 | `FinalAccountSide=1` 的 Party 净应收；BankAccount 余额；Cash 手头现金 |
| Debit 借方 | `FinalAccountSide=2` 的 Party 净应付 |
| Shufen | 净额正→Debit；净额负→Credit（或按净额绝对值两边展示） |

**利润：**

```
Profit = SUM(Credit) - SUM(Debit) - SUM(Expense on @date or period)
```

### 5.4 利润核算（问卷公式）

```
USDT 成本 BDT 价 = UnitPrice (如 126.10)
RMB 成本 BDT 价 = USDT成本 / Shufen汇率 (如 6.775) ≈ 18.612
卖 RMB 价 = UnitPrice (如 18.65)
毛利 BDT/RMB ≈ 卖价 - 成本价
```

报表层按 `SELL_RMB` 与 `BUY_USDT`/`USDT_TO_RMB` 关联日期区间汇总。

---

## 6. 改错原则

1. **不物理删除**历史流水；使用 `IsDeleted=1` 或 `ADJUSTMENT` 冲正  
2. 冲正记录填写 `ReversedTransId` 指向原单  
3. 所有调整必须填写 `Remark`

---

## 7. 执行顺序

```text
1. 创建数据库 LedGerDB（若未建）
2. 执行 01_LedGerDB_Schema.sql
3. 执行 02_LedGerDB_SeedData.sql
4. 应用首次启动：为 shamim 设置真实密码哈希
5. 上线日：录入 OpeningBalance（或以 OPENING_BALANCE 流水录入）
```

---

## 8. 连接串（开发环境，勿提交 Git）

```
Data Source=.;Database=LedGerDB;User id=dashi;PWD=***;pooling=true;min pool size=10;max pool size=1000;
```
