# LedGerDB 数据库脚本说明

## 执行顺序

```text
01_LedGerDB_Schema.sql
02_LedGerDB_SeedData.sql
04_LedGerDB_ColumnDescriptions.sql   ← 字段说明（必跑）
03_AccountingRules.md                  ← 记账规则文档（不用执行）
```

## 在 SSMS 中查看字段说明

表设计器网格里**不会直接显示**说明文字，按下面步骤：

1. 右键表 → **设计(Design)**
2. 在列网格中**单击某一列**（如 `Amount`）
3. 打开下方 **列属性(Column Properties)** 窗口
4. 展开 **说明(Description)** 属性

或用 SQL 查询：

```sql
SELECT
    t.name  AS TableName,
    c.name  AS ColumnName,
    CAST(ep.value AS NVARCHAR(500)) AS Description
FROM sys.tables t
JOIN sys.columns c ON c.object_id = t.object_id
LEFT JOIN sys.extended_properties ep
    ON ep.major_id = t.object_id
   AND ep.minor_id = c.column_id
   AND ep.name = N'MS_Description'
WHERE t.schema_id = SCHEMA_ID('dbo')
ORDER BY t.name, c.column_id;
```

## 04 脚本说明

- 覆盖 **6 张表 + 全部字段** 的 `MS_Description`（中英对照）
- 可重复执行（已存在则更新）
- 建议在 **SSMS 里 F5 执行**；若用 `sqlcmd`，文件须为 **UTF-8 BOM** 编码

## 当前库应看到

执行完成后 `MS_Description` 约 **93 条**（6 个表说明 + 87 个字段说明）。
