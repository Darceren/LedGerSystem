using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface ITransactionService
{
    Task<(bool Success, string? Error, LedgerTransaction? Transaction)> CreateAsync(TransactionEntryViewModel model, long userId);

    Task<List<LedgerTransaction>> GetRecentAsync(int take = 20);

    Task<TransactionListViewModel> GetListAsync(TransactionQueryModel query);

    Task<LedgerTransaction?> GetByIdAsync(long id);

    Task<(bool Success, string? Error, LedgerTransaction? Transaction)> CreateReversalAsync(long originalId, string remark, long userId);
}

public class TransactionService(ISqlSugarClient db) : ITransactionService
{
    public async Task<(bool Success, string? Error, LedgerTransaction? Transaction)> CreateAsync(
        TransactionEntryViewModel model,
        long userId)
    {
        var transType = await db.Queryable<SysTransactionType>()
            .FirstAsync(x => x.Id == model.TransTypeId && x.IsActive);

        if (transType is null)
        {
            return (false, "Invalid transaction type.", null);
        }

        ApplyDefaults(model, transType);

        var validationError = Validate(model, transType);
        if (validationError is not null)
        {
            return (false, validationError, null);
        }

        var now = DateTime.UtcNow;
        var entity = new LedgerTransaction
        {
            TransNo = await GenerateTransNoAsync(model.TransDate),
            TransDate = model.TransDate,
            TransTypeId = model.TransTypeId,
            PartyId = model.PartyId,
            RelatedPartyId = model.RelatedPartyId,
            BankAccountId = model.BankAccountId,
            Amount = model.Amount,
            Currency = model.Currency,
            UnitPrice = model.UnitPrice,
            Quantity = model.Quantity,
            QuantityCurrency = model.QuantityCurrency,
            EquivalentAmount = model.EquivalentAmount,
            EquivalentCurrency = model.EquivalentCurrency,
            PaymentMode = model.PaymentMode,
            PayMethod = model.PayMethod,
            PayoutChannel = model.PayoutChannel,
            PayoutAccountName = model.PayoutAccountName,
            PayoutAccountNo = model.PayoutAccountNo,
            Remark = model.Remark,
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        var id = await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
        entity.Id = id;

        var saved = await db.Queryable<LedgerTransaction>()
            .Includes(x => x.TransType)
            .Includes(x => x.Party)
            .Includes(x => x.RelatedParty)
            .Includes(x => x.BankAccount)
            .FirstAsync(x => x.Id == id);

        return (true, null, saved);
    }

    public Task<List<LedgerTransaction>> GetRecentAsync(int take = 20)
    {
        return db.Queryable<LedgerTransaction>()
            .Includes(x => x.TransType)
            .Includes(x => x.Party)
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.TransDate, OrderByType.Desc)
            .Take(take)
            .ToListAsync();
    }

    public async Task<TransactionListViewModel> GetListAsync(TransactionQueryModel query)
    {
        var q = db.Queryable<LedgerTransaction>()
            .Includes(x => x.TransType)
            .Includes(x => x.Party)
            .Includes(x => x.RelatedParty)
            .Includes(x => x.BankAccount)
            .Where(x => !x.IsDeleted);

        if (query.DateFrom.HasValue)
        {
            q = q.Where(x => x.TransDate >= query.DateFrom.Value.Date);
        }

        if (query.DateTo.HasValue)
        {
            var end = query.DateTo.Value.Date.AddDays(1);
            q = q.Where(x => x.TransDate < end);
        }

        if (query.TransTypeId.HasValue)
        {
            q = q.Where(x => x.TransTypeId == query.TransTypeId.Value);
        }

        if (query.PartyId.HasValue)
        {
            var partyId = query.PartyId.Value;
            q = q.Where(x => x.PartyId == partyId || x.RelatedPartyId == partyId);
        }

        if (!string.IsNullOrWhiteSpace(query.TransNo))
        {
            q = q.Where(x => x.TransNo != null && x.TransNo.Contains(query.TransNo));
        }

        var total = await q.CountAsync();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 10, 100);

        var items = await q
            .OrderBy(x => x.TransDate, OrderByType.Desc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        query.Page = page;
        query.PageSize = pageSize;

        return new TransactionListViewModel
        {
            Query = query,
            Items = items,
            TotalCount = total
        };
    }

    public async Task<LedgerTransaction?> GetByIdAsync(long id)
    {
        var items = await db.Queryable<LedgerTransaction>()
            .Includes(x => x.TransType)
            .Includes(x => x.Party)
            .Includes(x => x.RelatedParty)
            .Includes(x => x.BankAccount)
            .Where(x => x.Id == id)
            .Take(1)
            .ToListAsync();
        return items.FirstOrDefault();
    }

    public async Task<(bool Success, string? Error, LedgerTransaction? Transaction)> CreateReversalAsync(
        long originalId,
        string remark,
        long userId)
    {
        if (string.IsNullOrWhiteSpace(remark))
        {
            return (false, "Please enter a reason for reversal.", null);
        }

        var original = await GetByIdAsync(originalId);
        if (original is null || original.IsDeleted)
        {
            return (false, "Transaction not found.", null);
        }

        if (original.IsAdjustment)
        {
            return (false, "Cannot reverse an adjustment entry.", null);
        }

        var alreadyReversed = await db.Queryable<LedgerTransaction>()
            .AnyAsync(x => !x.IsDeleted && x.ReversedTransId == originalId);
        if (alreadyReversed)
        {
            return (false, "This transaction has already been reversed.", null);
        }

        var adjustType = await db.Queryable<SysTransactionType>()
            .FirstAsync(x => x.Code == "ADJUSTMENT" && x.IsActive);
        if (adjustType is null)
        {
            return (false, "Adjustment type not configured.", null);
        }

        var now = DateTime.UtcNow;
        original.IsDeleted = true;
        original.UpdatedAt = now;
        await db.Updateable(original).UpdateColumns(x => new { x.IsDeleted, x.UpdatedAt }).ExecuteCommandAsync();

        var audit = new LedgerTransaction
        {
            TransNo = await GenerateTransNoAsync(DateTime.Now),
            TransDate = DateTime.Now,
            TransTypeId = adjustType.Id,
            PartyId = original.PartyId,
            RelatedPartyId = original.RelatedPartyId,
            BankAccountId = original.BankAccountId,
            Amount = original.Amount,
            Currency = original.Currency,
            UnitPrice = original.UnitPrice,
            Quantity = original.Quantity,
            QuantityCurrency = original.QuantityCurrency,
            EquivalentAmount = original.EquivalentAmount,
            EquivalentCurrency = original.EquivalentCurrency,
            PaymentMode = original.PaymentMode,
            PayMethod = original.PayMethod,
            PayoutChannel = original.PayoutChannel,
            IsAdjustment = true,
            ReversedTransId = original.Id,
            Remark = $"Reversal of {original.TransNo}: {remark.Trim()}",
            CreatedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            IsDeleted = false
        };

        var id = await db.Insertable(audit).ExecuteReturnBigIdentityAsync();
        var saved = await GetByIdAsync(id);
        return (true, null, saved);
    }

    private static string? Validate(TransactionEntryViewModel model, SysTransactionType type)
    {
        if (model.Amount <= 0)
        {
            return "Amount must be greater than zero.";
        }

        if (type.RequireParty && !model.PartyId.HasValue)
        {
            return "Please select a party.";
        }

        if (type.RequireRelatedParty && !model.RelatedPartyId.HasValue)
        {
            return "Please select a related supplier.";
        }

        if (type.RequireBankAccount && !model.BankAccountId.HasValue)
        {
            return "Please select a bank account.";
        }

        if (type.RequirePaymentMode && !model.PaymentMode.HasValue)
        {
            return "Please select a payment mode.";
        }

        if (type.RequirePayoutChannel && !model.PayoutChannel.HasValue)
        {
            return "Please select a payout channel.";
        }

        return type.Code switch
        {
            "BUY_USDT" when !model.UnitPrice.HasValue || model.UnitPrice <= 0 => "Please enter cost (BDT/USDT).",
            "BUY_USDT" when !model.Quantity.HasValue || model.Quantity <= 0 => "Please enter USDT quantity.",
            "BUY_USDT" when !model.PayMethod.HasValue => "Please select pay method (Cash/Bank).",
            "BUY_USDT" when model.PayMethod == 2 && !model.BankAccountId.HasValue => "Please select bank account for payment.",
            "USDT_TO_RMB" when !model.Quantity.HasValue || model.Quantity <= 0 => "Please enter USDT quantity.",
            "USDT_TO_RMB" when !model.EquivalentAmount.HasValue || model.EquivalentAmount <= 0 => "Please enter RMB received amount.",
            "SELL_RMB" when model.Currency != AppConstants.CurrencyCny => "Sell RMB amount currency must be CNY.",
            "PAY_BDT_SUPPLIER" when !model.PayMethod.HasValue => "Please select pay method (Cash/Bank).",
            "PAY_BDT_SUPPLIER" when model.PayMethod == 2 && !model.BankAccountId.HasValue => "Please select bank account for payment.",
            _ => null
        };
    }

    private static void ApplyDefaults(TransactionEntryViewModel model, SysTransactionType type)
    {
        switch (type.Code)
        {
            case "COLLECT_BDT_CASH":
                model.Currency = AppConstants.CurrencyBdt;
                model.PaymentMode = AppConstants.PaymentModeCash;
                break;
            case "COLLECT_BDT_BANK":
                model.Currency = AppConstants.CurrencyBdt;
                model.PaymentMode = AppConstants.PaymentModeMyBank;
                break;
            case "COLLECT_BDT_SUPPLIER":
                model.Currency = AppConstants.CurrencyBdt;
                model.PaymentMode = AppConstants.PaymentModeSupplierBank;
                break;
            case "PAY_BDT_SUPPLIER":
            case "EXPENSE":
                model.Currency = AppConstants.CurrencyBdt;
                break;
            case "SELL_RMB":
                model.Currency = AppConstants.CurrencyCny;
                if (model.UnitPrice.HasValue && model.Amount > 0)
                {
                    model.EquivalentAmount ??= Math.Round(model.Amount * model.UnitPrice.Value, 4);
                    model.EquivalentCurrency = AppConstants.CurrencyBdt;
                }
                break;
            case "BUY_USDT":
                model.Currency = AppConstants.CurrencyBdt;
                model.QuantityCurrency = AppConstants.CurrencyUsdt;
                if ((!model.Quantity.HasValue || model.Quantity <= 0)
                    && model.UnitPrice is > 0
                    && model.Amount > 0)
                {
                    model.Quantity = Math.Round(model.Amount / model.UnitPrice.Value, 4);
                }
                break;
            case "USDT_TO_RMB":
                model.QuantityCurrency = AppConstants.CurrencyUsdt;
                model.EquivalentCurrency = AppConstants.CurrencyCny;
                break;
        }
    }

    private async Task<string> GenerateTransNoAsync(DateTime transDate)
    {
        var prefix = $"T{transDate:yyyyMMdd}";
        var count = await db.Queryable<LedgerTransaction>()
            .Where(x => x.TransNo != null && x.TransNo.StartsWith(prefix))
            .CountAsync();

        return $"{prefix}{(count + 1):D4}";
    }
}
