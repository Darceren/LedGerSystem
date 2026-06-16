using LedGerSystem.Entities;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IOpeningBalanceService
{
    Task<OpeningBalancePageModel> GetPageAsync(OpeningBalanceEditViewModel? form = null);

    Task<OpeningBalance?> GetByIdAsync(long id);

    Task<(bool Success, string? Error)> SaveAsync(OpeningBalanceEditViewModel model, long userId);

    Task<(bool Success, string? Error)> DeleteAsync(long id);
}

public class OpeningBalanceService(ISqlSugarClient db) : IOpeningBalanceService
{
    public async Task<OpeningBalancePageModel> GetPageAsync(OpeningBalanceEditViewModel? form = null)
    {
        var items = await db.Queryable<OpeningBalance>()
            .OrderBy(x => x.BalanceDate, OrderByType.Desc)
            .OrderBy(x => x.Id, OrderByType.Desc)
            .ToListAsync();

        var parties = await db.Queryable<Party>().ToListAsync();
        var banks = await db.Queryable<BankAccount>().ToListAsync();
        var partyMap = parties.ToDictionary(x => x.Id);
        var bankMap = banks.ToDictionary(x => x.Id);

        return new OpeningBalancePageModel
        {
            Items = items.Select(x => MapListItem(x, partyMap, bankMap)).ToList(),
            Form = form ?? new OpeningBalanceEditViewModel()
        };
    }

    public async Task<OpeningBalance?> GetByIdAsync(long id)
    {
        var items = await db.Queryable<OpeningBalance>().Where(x => x.Id == id).Take(1).ToListAsync();
        return items.FirstOrDefault();
    }

    public async Task<(bool Success, string? Error)> SaveAsync(OpeningBalanceEditViewModel model, long userId)
    {
        var error = Validate(model);
        if (error is not null)
        {
            return (false, error);
        }

        var now = DateTime.UtcNow;
        if (model.Id.HasValue && model.Id > 0)
        {
            var existing = await db.Queryable<OpeningBalance>().FirstAsync(x => x.Id == model.Id);
            if (existing is null)
            {
                return (false, "Opening balance not found.");
            }

            existing.BalanceDate = model.BalanceDate.Date;
            existing.EntityType = model.EntityType;
            existing.PartyId = model.EntityType == 1 ? model.PartyId : null;
            existing.BankAccountId = model.EntityType == 2 ? model.BankAccountId : null;
            existing.Currency = model.Currency;
            existing.Amount = model.Amount;
            existing.Remark = model.Remark;

            await db.Updateable(existing).ExecuteCommandAsync();
            return (true, null);
        }

        var entity = new OpeningBalance
        {
            BalanceDate = model.BalanceDate.Date,
            EntityType = model.EntityType,
            PartyId = model.EntityType == 1 ? model.PartyId : null,
            BankAccountId = model.EntityType == 2 ? model.BankAccountId : null,
            Currency = model.Currency,
            Amount = model.Amount,
            Remark = model.Remark,
            CreatedByUserId = userId,
            CreatedAt = now
        };

        await db.Insertable(entity).ExecuteReturnBigIdentityAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(long id)
    {
        var rows = await db.Deleteable<OpeningBalance>().Where(x => x.Id == id).ExecuteCommandAsync();
        return rows > 0 ? (true, null) : (false, "Opening balance not found.");
    }

    private static string? Validate(OpeningBalanceEditViewModel model)
    {
        if (model.EntityType == 1 && !model.PartyId.HasValue)
        {
            return "Please select a party.";
        }

        if (model.EntityType == 2 && !model.BankAccountId.HasValue)
        {
            return "Please select a bank account.";
        }

        if (model.Amount == 0)
        {
            return "Amount cannot be zero.";
        }

        return null;
    }

    private static OpeningBalanceListItem MapListItem(
        OpeningBalance ob,
        Dictionary<long, Party> partyMap,
        Dictionary<int, BankAccount> bankMap)
    {
        var item = new OpeningBalanceListItem
        {
            Id = ob.Id,
            BalanceDate = ob.BalanceDate,
            EntityType = ob.EntityType,
            Currency = ob.Currency,
            Amount = ob.Amount,
            Remark = ob.Remark
        };

        switch (ob.EntityType)
        {
            case 1 when ob.PartyId.HasValue && partyMap.TryGetValue(ob.PartyId.Value, out var party):
                item.EntityTypeName = "Party";
                item.EntityCode = party.Code;
                item.EntityName = party.Name;
                break;
            case 2 when ob.BankAccountId.HasValue && bankMap.TryGetValue(ob.BankAccountId.Value, out var bank):
                item.EntityTypeName = "Bank";
                item.EntityCode = bank.Code;
                item.EntityName = bank.Name;
                break;
            case 3:
                item.EntityTypeName = "Cash";
                item.EntityCode = "CASH";
                item.EntityName = "Cash on Hand";
                break;
            default:
                item.EntityTypeName = "Unknown";
                break;
        }

        return item;
    }
}
