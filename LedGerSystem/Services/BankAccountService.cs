using LedGerSystem.Entities;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IBankAccountService
{
    Task<List<BankAccount>> GetListAsync(bool includeInactive = false);

    Task<BankAccount?> GetByIdAsync(int id);

    Task<(bool Success, string? Error)> SaveAsync(BankAccountEditViewModel model);
}

public class BankAccountService(ISqlSugarClient db) : IBankAccountService
{
    public Task<List<BankAccount>> GetListAsync(bool includeInactive = false)
    {
        var query = db.Queryable<BankAccount>();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        return query.OrderBy(x => x.SortOrder).OrderBy(x => x.Code).ToListAsync();
    }

    public async Task<BankAccount?> GetByIdAsync(int id)
    {
        return await db.Queryable<BankAccount>().InSingleAsync(id);
    }

    public async Task<(bool Success, string? Error)> SaveAsync(BankAccountEditViewModel model)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Code is required.");
        }

        var exists = await db.Queryable<BankAccount>()
            .Where(x => x.Code == code && x.Id != model.Id)
            .AnyAsync();
        if (exists)
        {
            return (false, $"Code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        if (model.Id == 0)
        {
            await db.Insertable(new BankAccount
            {
                Code = code,
                Name = model.Name.Trim(),
                BankName = model.BankName,
                AccountNo = model.AccountNo,
                Currency = model.Currency,
                FinalAccountSide = model.FinalAccountSide,
                IsActive = model.IsActive,
                SortOrder = model.SortOrder,
                Remark = model.Remark,
                CreatedAt = now,
                UpdatedAt = now
            }).ExecuteCommandAsync();
        }
        else
        {
            var entity = await db.Queryable<BankAccount>().InSingleAsync(model.Id);
            if (entity is null)
            {
                return (false, "Bank account not found.");
            }

            entity.Code = code;
            entity.Name = model.Name.Trim();
            entity.BankName = model.BankName;
            entity.AccountNo = model.AccountNo;
            entity.Currency = model.Currency;
            entity.FinalAccountSide = model.FinalAccountSide;
            entity.IsActive = model.IsActive;
            entity.SortOrder = model.SortOrder;
            entity.Remark = model.Remark;
            entity.UpdatedAt = now;

            await db.Updateable(entity).ExecuteCommandAsync();
        }

        return (true, null);
    }
}
