using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IPartyService
{
    Task<List<Party>> GetListAsync(byte? partyType = null, bool includeInactive = false);

    Task<Party?> GetByIdAsync(long id);

    Task<(bool Success, string? Error)> SaveAsync(PartyEditViewModel model);
}

public class PartyService(ISqlSugarClient db) : IPartyService
{
    public Task<List<Party>> GetListAsync(byte? partyType = null, bool includeInactive = false)
    {
        var query = db.Queryable<Party>();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (partyType.HasValue)
        {
            query = query.Where(x => x.PartyType == partyType.Value);
        }

        return query.OrderBy(x => x.SortOrder).OrderBy(x => x.Code).ToListAsync();
    }

    public async Task<Party?> GetByIdAsync(long id)
    {
        return await db.Queryable<Party>().InSingleAsync(id);
    }

    public async Task<(bool Success, string? Error)> SaveAsync(PartyEditViewModel model)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Code is required.");
        }

        var exists = await db.Queryable<Party>()
            .Where(x => x.Code == code && x.Id != model.Id)
            .AnyAsync();
        if (exists)
        {
            return (false, $"Code '{code}' already exists.");
        }

        var now = DateTime.UtcNow;
        if (model.Id == 0)
        {
            var entity = new Party
            {
                Code = code,
                Name = model.Name.Trim(),
                PartyType = model.PartyType,
                FinalAccountSide = model.FinalAccountSide,
                ContactPhone = model.ContactPhone,
                Remark = model.Remark,
                IsActive = model.IsActive,
                SortOrder = model.SortOrder,
                CreatedAt = now,
                UpdatedAt = now
            };

            ApplyDefaultFinalSide(entity);
            await db.Insertable(entity).ExecuteCommandAsync();
        }
        else
        {
            var entity = await db.Queryable<Party>().InSingleAsync(model.Id);
            if (entity is null)
            {
                return (false, "Party not found.");
            }

            entity.Code = code;
            entity.Name = model.Name.Trim();
            entity.PartyType = model.PartyType;
            entity.FinalAccountSide = model.FinalAccountSide;
            entity.ContactPhone = model.ContactPhone;
            entity.Remark = model.Remark;
            entity.IsActive = model.IsActive;
            entity.SortOrder = model.SortOrder;
            entity.UpdatedAt = now;

            await db.Updateable(entity).ExecuteCommandAsync();
        }

        return (true, null);
    }

    private static void ApplyDefaultFinalSide(Party entity)
    {
        if (entity.FinalAccountSide != 0)
        {
            return;
        }

        entity.FinalAccountSide = entity.PartyType switch
        {
            AppConstants.PartyTypeCustomer => (byte)1,
            AppConstants.PartyTypeSupplier => (byte)2,
            _ => (byte)0
        };
    }
}
