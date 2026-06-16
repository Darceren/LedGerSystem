using LedGerSystem.Entities;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using SqlSugar;

namespace LedGerSystem.Services;

public interface IReportService
{
    Task<DailyRmbPayoutViewModel> GetDailyRmbPayoutAsync(DateTime date);

    Task<DailyBdtCollectionViewModel> GetDailyBdtCollectionAsync(DateTime date);

    Task<ProfitAnalysisViewModel> GetProfitAnalysisAsync(DateTime dateFrom, DateTime dateTo);

    Task<decimal> GetTodayBdtCollectedAsync(DateTime date);

    Task<int> GetTodayTransactionCountAsync(DateTime date);
}

public class ReportService(ISqlSugarClient db) : IReportService
{
    public async Task<DailyRmbPayoutViewModel> GetDailyRmbPayoutAsync(DateTime date)
    {
        var day = date.Date;
        var typeId = await GetTypeIdAsync("SELL_RMB");

        var rows = await db.Queryable<LedgerTransaction>()
            .Includes(x => x.Party)
            .Where(x => !x.IsDeleted && x.TransTypeId == typeId && x.TransDate >= day && x.TransDate < day.AddDays(1))
            .ToListAsync();

        var lines = rows
            .GroupBy(x => x.PartyId)
            .Select(g =>
            {
                var party = g.First().Party;
                return new DailyPartyAmountLine
                {
                    PartyCode = party?.Code ?? "-",
                    PartyName = party?.Name ?? "-",
                    Amount = g.Sum(x => x.Amount),
                    TxCount = g.Count(),
                    Currency = AppConstants.CurrencyCny
                };
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new DailyRmbPayoutViewModel
        {
            ReportDate = day,
            Lines = lines,
            TotalRmb = lines.Sum(x => x.Amount),
            TotalCount = rows.Count
        };
    }

    public async Task<DailyBdtCollectionViewModel> GetDailyBdtCollectionAsync(DateTime date)
    {
        var day = date.Date;
        var codes = new[] { "COLLECT_BDT_CASH", "COLLECT_BDT_BANK", "COLLECT_BDT_SUPPLIER" };
        var typeIds = await db.Queryable<SysTransactionType>()
            .Where(x => codes.Contains(x.Code))
            .Select(x => x.Id)
            .ToListAsync();

        var rows = await db.Queryable<LedgerTransaction>()
            .Includes(x => x.Party)
            .Includes(x => x.TransType)
            .Where(x => !x.IsDeleted && typeIds.Contains(x.TransTypeId) && x.TransDate >= day && x.TransDate < day.AddDays(1))
            .ToListAsync();

        var lines = rows
            .GroupBy(x => new { x.PartyId, x.PaymentMode })
            .Select(g =>
            {
                var party = g.First().Party;
                return new DailyBdtCollectionLine
                {
                    PartyCode = party?.Code ?? "-",
                    PartyName = party?.Name ?? "-",
                    PaymentModeName = GetPaymentModeName(g.Key.PaymentMode),
                    Amount = g.Sum(x => x.Amount),
                    TxCount = g.Count()
                };
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new DailyBdtCollectionViewModel
        {
            ReportDate = day,
            Lines = lines,
            TotalBdt = lines.Sum(x => x.Amount)
        };
    }

    public async Task<ProfitAnalysisViewModel> GetProfitAnalysisAsync(DateTime dateFrom, DateTime dateTo)
    {
        var from = dateFrom.Date;
        var to = dateTo.Date.AddDays(1).AddTicks(-1);

        var buyTypeId = await GetTypeIdAsync("BUY_USDT");
        var shufenTypeId = await GetTypeIdAsync("USDT_TO_RMB");
        var sellTypeId = await GetTypeIdAsync("SELL_RMB");

        var buys = await db.Queryable<LedgerTransaction>()
            .Where(x => !x.IsDeleted && x.TransTypeId == buyTypeId && x.TransDate >= from && x.TransDate <= to)
            .ToListAsync();

        var shufen = await db.Queryable<LedgerTransaction>()
            .Where(x => !x.IsDeleted && x.TransTypeId == shufenTypeId && x.TransDate >= from && x.TransDate <= to)
            .ToListAsync();

        var sells = await db.Queryable<LedgerTransaction>()
            .Includes(x => x.Party)
            .Where(x => !x.IsDeleted && x.TransTypeId == sellTypeId && x.TransDate >= from && x.TransDate <= to)
            .ToListAsync();

        var totalUsdt = buys.Sum(x => x.Quantity ?? 0);
        var totalBdtPaid = buys.Sum(x => x.Amount);
        var avgUsdtCost = totalUsdt > 0 ? totalBdtPaid / totalUsdt : 0;

        var totalRmb = shufen.Sum(x => x.EquivalentAmount ?? 0);
        var totalUsdtToShufen = shufen.Sum(x => x.Quantity ?? 0);
        var avgShufenRate = totalUsdtToShufen > 0 ? totalRmb / totalUsdtToShufen : 0;
        var avgRmbCostBdt = avgShufenRate > 0 ? avgUsdtCost / avgShufenRate : 0;

        var sellLines = sells.Select(x =>
        {
            var sellRate = x.UnitPrice ?? 0;
            var bdtRecv = x.EquivalentAmount ?? (sellRate > 0 ? x.Amount * sellRate : 0);
            var estProfitPerRmb = sellRate - avgRmbCostBdt;
            return new ProfitAnalysisLine
            {
                TransDate = x.TransDate,
                PartyCode = x.Party?.Code ?? "-",
                RmbAmount = x.Amount,
                SellRate = sellRate,
                BdtReceivable = bdtRecv,
                EstCostBdtPerRmb = avgRmbCostBdt,
                EstGrossProfitBdt = estProfitPerRmb * x.Amount
            };
        }).ToList();

        return new ProfitAnalysisViewModel
        {
            DateFrom = from,
            DateTo = dateTo.Date,
            TotalUsdtBought = totalUsdt,
            TotalBdtPaidForUsdt = totalBdtPaid,
            AvgUsdtCostBdt = avgUsdtCost,
            TotalRmbFromShufen = totalRmb,
            AvgRmbCostBdt = avgRmbCostBdt,
            TotalRmbSold = sells.Sum(x => x.Amount),
            TotalBdtReceivableFromSales = sellLines.Sum(x => x.BdtReceivable),
            AvgSellRateBdtPerRmb = sells.Where(x => x.UnitPrice.HasValue).Select(x => x.UnitPrice!.Value).DefaultIfEmpty(0).Average(),
            EstimatedGrossProfitBdt = sellLines.Sum(x => x.EstGrossProfitBdt),
            SellLines = sellLines
        };
    }

    public async Task<decimal> GetTodayBdtCollectedAsync(DateTime date)
    {
        var day = date.Date;
        var codes = new[] { "COLLECT_BDT_CASH", "COLLECT_BDT_BANK", "COLLECT_BDT_SUPPLIER" };
        var typeIds = await db.Queryable<SysTransactionType>()
            .Where(x => codes.Contains(x.Code))
            .Select(x => x.Id)
            .ToListAsync();

        return await db.Queryable<LedgerTransaction>()
            .Where(x => !x.IsDeleted && typeIds.Contains(x.TransTypeId) && x.TransDate >= day && x.TransDate < day.AddDays(1))
            .SumAsync(x => x.Amount);
    }

    public async Task<int> GetTodayTransactionCountAsync(DateTime date)
    {
        var day = date.Date;
        return await db.Queryable<LedgerTransaction>()
            .Where(x => !x.IsDeleted && x.TransDate >= day && x.TransDate < day.AddDays(1))
            .CountAsync();
    }

    private async Task<int> GetTypeIdAsync(string code)
    {
        return await db.Queryable<SysTransactionType>()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .FirstAsync();
    }

    private static string GetPaymentModeName(byte? mode) => mode switch
    {
        AppConstants.PaymentModeCash => "Cash",
        AppConstants.PaymentModeMyBank => "My Bank",
        AppConstants.PaymentModeSupplierBank => "Supplier Bank",
        _ => "Unknown"
    };
}
