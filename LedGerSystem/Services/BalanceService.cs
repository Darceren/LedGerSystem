using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IBalanceService
{
    Task<BalanceSnapshot> GetSnapshotAsync(DateTime? asOfDate = null);

    Task<FinalAccountViewModel> GetFinalAccountAsync(DateTime asOfDate);
}

public class BalanceSnapshot
{
    public decimal CashOnHand { get; set; }

    public Dictionary<long, decimal> PartyBalances { get; set; } = [];

    public Dictionary<int, decimal> BankBalances { get; set; } = [];

    public decimal TotalExpenses { get; set; }
}

public class BalanceService(ISqlSugarClient db) : IBalanceService
{
    public async Task<BalanceSnapshot> GetSnapshotAsync(DateTime? asOfDate = null)
    {
        var cutoff = (asOfDate ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);
        var snapshot = new BalanceSnapshot();

        var openings = await db.Queryable<OpeningBalance>()
            .Where(x => x.BalanceDate <= cutoff.Date)
            .ToListAsync();

        foreach (var ob in openings)
        {
            ApplyOpening(snapshot, ob);
        }

        var typeMap = await db.Queryable<SysTransactionType>().ToListAsync();
        var typeById = typeMap.ToDictionary(x => x.Id, x => x.Code);

        var transactions = await db.Queryable<LedgerTransaction>()
            .Where(x => !x.IsDeleted && x.TransDate <= cutoff)
            .OrderBy(x => x.TransDate)
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var tx in transactions)
        {
            if (tx.IsAdjustment && tx.ReversedTransId.HasValue)
            {
                continue;
            }

            if (!typeById.TryGetValue(tx.TransTypeId, out var code))
            {
                continue;
            }

            ApplyTransaction(snapshot, tx, code);
        }

        return snapshot;
    }

    public async Task<FinalAccountViewModel> GetFinalAccountAsync(DateTime asOfDate)
    {
        var snapshot = await GetSnapshotAsync(asOfDate);
        var parties = await db.Queryable<Party>().Where(x => x.IsActive).ToListAsync();
        var banks = await db.Queryable<BankAccount>().Where(x => x.IsActive).ToListAsync();
        var partyMap = parties.ToDictionary(x => x.Id);

        var model = new FinalAccountViewModel
        {
            AsOfDate = asOfDate.Date,
            TotalExpenses = snapshot.TotalExpenses
        };

        if (snapshot.CashOnHand != 0)
        {
            model.CreditLines.Add(new BalanceLineItem
            {
                Code = "CASH",
                Name = "Cash on Hand",
                Category = "Cash",
                Balance = snapshot.CashOnHand,
                Currency = AppConstants.CurrencyBdt
            });
        }

        foreach (var bank in banks)
        {
            if (!snapshot.BankBalances.TryGetValue(bank.Id, out var bal) || bal == 0)
            {
                continue;
            }

            var line = new BalanceLineItem
            {
                Code = bank.Code,
                Name = bank.Name,
                Category = "Bank",
                Balance = bal,
                Currency = bank.Currency
            };

            if (bank.FinalAccountSide == 2)
            {
                model.DebitLines.Add(line);
            }
            else
            {
                model.CreditLines.Add(line);
            }
        }

        foreach (var (partyId, balance) in snapshot.PartyBalances)
        {
            if (balance == 0 || !partyMap.TryGetValue(partyId, out var party))
            {
                continue;
            }

            var line = new BalanceLineItem
            {
                Code = party.Code,
                Name = party.Name,
                Category = GetPartyTypeName(party.PartyType),
                Balance = Math.Abs(balance),
                Currency = AppConstants.CurrencyBdt
            };

            var side = party.FinalAccountSide;
            if (side == 0)
            {
                side = party.PartyType switch
                {
                    AppConstants.PartyTypeCustomer => (byte)1,
                    AppConstants.PartyTypeSupplier => (byte)2,
                    AppConstants.PartyTypeShufen => balance >= 0 ? (byte)2 : (byte)1,
                    _ => balance >= 0 ? (byte)1 : (byte)2
                };
            }

            if (party.PartyType == AppConstants.PartyTypeShufen && party.FinalAccountSide == 0)
            {
                if (balance >= 0)
                {
                    model.DebitLines.Add(line);
                }
                else
                {
                    model.CreditLines.Add(line);
                }
            }
            else if (side == 1)
            {
                if (balance > 0)
                {
                    model.CreditLines.Add(line);
                }
                else if (balance < 0)
                {
                    model.DebitLines.Add(new BalanceLineItem
                    {
                        Code = line.Code,
                        Name = line.Name,
                        Category = line.Category,
                        Balance = Math.Abs(balance),
                        Currency = line.Currency
                    });
                }
            }
            else if (side == 2)
            {
                if (balance > 0)
                {
                    model.DebitLines.Add(line);
                }
                else if (balance < 0)
                {
                    model.CreditLines.Add(new BalanceLineItem
                    {
                        Code = line.Code,
                        Name = line.Name,
                        Category = line.Category,
                        Balance = Math.Abs(balance),
                        Currency = line.Currency
                    });
                }
            }
        }

        model.CreditLines = model.CreditLines.OrderBy(x => x.Code).ToList();
        model.DebitLines = model.DebitLines.OrderBy(x => x.Code).ToList();
        model.TotalCredit = model.CreditLines.Sum(x => x.Balance);
        model.TotalDebit = model.DebitLines.Sum(x => x.Balance);
        model.Profit = model.TotalCredit - model.TotalDebit - model.TotalExpenses;

        return model;
    }

    private static void ApplyOpening(BalanceSnapshot snapshot, OpeningBalance ob)
    {
        switch (ob.EntityType)
        {
            case 1 when ob.PartyId.HasValue:
                AddParty(snapshot, ob.PartyId.Value, ob.Amount);
                break;
            case 2 when ob.BankAccountId.HasValue:
                AddBank(snapshot, ob.BankAccountId.Value, ob.Amount);
                break;
            case 3:
                snapshot.CashOnHand += ob.Amount;
                break;
        }
    }

    private static void ApplyTransaction(BalanceSnapshot snapshot, LedgerTransaction tx, string code)
    {
        switch (code)
        {
            case "SELL_RMB":
                if (tx.PartyId.HasValue)
                {
                    var receivable = tx.EquivalentAmount ?? (tx.UnitPrice.HasValue ? tx.Amount * tx.UnitPrice.Value : tx.Amount);
                    AddParty(snapshot, tx.PartyId.Value, receivable);
                }
                break;

            case "COLLECT_BDT_CASH":
                snapshot.CashOnHand += tx.Amount;
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, -tx.Amount);
                }
                break;

            case "COLLECT_BDT_BANK":
                if (tx.BankAccountId.HasValue)
                {
                    AddBank(snapshot, tx.BankAccountId.Value, tx.Amount);
                }
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, -tx.Amount);
                }
                break;

            case "COLLECT_BDT_SUPPLIER":
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, -tx.Amount);
                }
                if (tx.RelatedPartyId.HasValue)
                {
                    AddParty(snapshot, tx.RelatedPartyId.Value, -tx.Amount);
                }
                break;

            case "PAY_BDT_SUPPLIER":
                DeductCashOrBank(snapshot, tx);
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, -tx.Amount);
                }
                break;

            case "BUY_USDT":
                DeductCashOrBank(snapshot, tx);
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, tx.Amount);
                }
                break;

            case "USDT_TO_RMB":
                if (tx.PartyId.HasValue)
                {
                    var rmbValue = tx.EquivalentAmount ?? 0;
                    var bdtValue = tx.UnitPrice.HasValue && rmbValue > 0
                        ? rmbValue * tx.UnitPrice.Value
                        : (tx.Quantity ?? 0);
                    AddParty(snapshot, tx.PartyId.Value, -bdtValue);
                }
                break;

            case "EXPENSE":
                snapshot.TotalExpenses += tx.Amount;
                DeductCashOrBank(snapshot, tx);
                break;

            case "ADJUSTMENT":
                if (tx.PartyId.HasValue)
                {
                    AddParty(snapshot, tx.PartyId.Value, tx.Amount);
                }
                if (tx.BankAccountId.HasValue)
                {
                    AddBank(snapshot, tx.BankAccountId.Value, -tx.Amount);
                }
                else if (tx.PayMethod == 1 || !tx.BankAccountId.HasValue)
                {
                    snapshot.CashOnHand -= tx.Amount;
                }
                break;
        }
    }

    private static void DeductCashOrBank(BalanceSnapshot snapshot, LedgerTransaction tx)
    {
        if (tx.PayMethod == 2 && tx.BankAccountId.HasValue)
        {
            AddBank(snapshot, tx.BankAccountId.Value, -tx.Amount);
        }
        else
        {
            snapshot.CashOnHand -= tx.Amount;
        }
    }

    private static void AddParty(BalanceSnapshot snapshot, long partyId, decimal delta)
    {
        snapshot.PartyBalances.TryGetValue(partyId, out var current);
        snapshot.PartyBalances[partyId] = current + delta;
    }

    private static void AddBank(BalanceSnapshot snapshot, int bankId, decimal delta)
    {
        snapshot.BankBalances.TryGetValue(bankId, out var current);
        snapshot.BankBalances[bankId] = current + delta;
    }

    private static string GetPartyTypeName(byte partyType) => partyType switch
    {
        AppConstants.PartyTypeCustomer => "Customer",
        AppConstants.PartyTypeSupplier => "Supplier",
        AppConstants.PartyTypeShufen => "Shufen",
        _ => "Other"
    };
}
