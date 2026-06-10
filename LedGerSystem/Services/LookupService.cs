using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface ILookupService
{
    Task<TransactionEntryLookups> GetEntryLookupsAsync();
}

public class TransactionEntryLookups
{
    public List<SysTransactionType> TransactionTypes { get; set; } = [];

    public List<Party> Customers { get; set; } = [];

    public List<Party> Suppliers { get; set; } = [];

    public List<Party> ShufenParties { get; set; } = [];

    public List<BankAccount> BankAccounts { get; set; } = [];
}

public class LookupService(ISqlSugarClient db) : ILookupService
{
    public async Task<TransactionEntryLookups> GetEntryLookupsAsync()
    {
        var types = await db.Queryable<SysTransactionType>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var parties = await db.Queryable<Party>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        var banks = await db.Queryable<BankAccount>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        return new TransactionEntryLookups
        {
            TransactionTypes = types,
            Customers = parties.Where(x => x.PartyType == AppConstants.PartyTypeCustomer).ToList(),
            Suppliers = parties.Where(x => x.PartyType == AppConstants.PartyTypeSupplier).ToList(),
            ShufenParties = parties.Where(x => x.PartyType == AppConstants.PartyTypeShufen).ToList(),
            BankAccounts = banks
        };
    }
}
