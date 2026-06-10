using LedGerSystem.Entities;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IBootstrapDataService
{
    Task EnsureSampleDataAsync();
}

public class BootstrapDataService(ISqlSugarClient db) : IBootstrapDataService
{
    public async Task EnsureSampleDataAsync()
    {
        if (!await db.Queryable<Party>().AnyAsync(x => x.PartyType == 1))
        {
            var now = DateTime.UtcNow;
            await db.Insertable(new Party
            {
                Code = "NYMT",
                Name = "Customer NYMT",
                PartyType = 1,
                FinalAccountSide = 1,
                IsActive = true,
                SortOrder = 100,
                CreatedAt = now,
                UpdatedAt = now
            }).ExecuteCommandAsync();
        }
    }
}
