using LedGerSystem.Models;
using MiniExcelLibs;

namespace LedGerSystem.Services;

public interface IExcelExportService
{
    byte[] ExportFinalAccount(FinalAccountViewModel model);

    byte[] ExportDailyRmbPayout(DailyRmbPayoutViewModel model);

    byte[] ExportDailyBdtCollection(DailyBdtCollectionViewModel model);

    byte[] ExportProfitAnalysis(ProfitAnalysisViewModel model);

    byte[] ExportTransactions(TransactionListViewModel model);
}

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportFinalAccount(FinalAccountViewModel model)
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Section"] = "Summary", ["Code"] = "", ["Name"] = "As of " + model.AsOfDate.ToString("yyyy-MM-dd"), ["Type"] = "", ["Amount"] = null },
            new() { ["Section"] = "Summary", ["Code"] = "CREDIT", ["Name"] = "Total Credit", ["Type"] = "", ["Amount"] = model.TotalCredit },
            new() { ["Section"] = "Summary", ["Code"] = "DEBIT", ["Name"] = "Total Debit", ["Type"] = "", ["Amount"] = model.TotalDebit },
            new() { ["Section"] = "Summary", ["Code"] = "EXPENSE", ["Name"] = "Expenses", ["Type"] = "", ["Amount"] = model.TotalExpenses },
            new() { ["Section"] = "Summary", ["Code"] = "PROFIT", ["Name"] = "Profit", ["Type"] = "", ["Amount"] = model.Profit }
        };

        foreach (var line in model.CreditLines)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["Section"] = "Credit",
                ["Code"] = line.Code,
                ["Name"] = line.Name,
                ["Type"] = line.Category,
                ["Amount"] = line.Balance
            });
        }

        foreach (var line in model.DebitLines)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["Section"] = "Debit",
                ["Code"] = line.Code,
                ["Name"] = line.Name,
                ["Type"] = line.Category,
                ["Amount"] = line.Balance
            });
        }

        return SaveAsBytes(rows);
    }

    public byte[] ExportDailyRmbPayout(DailyRmbPayoutViewModel model)
    {
        var rows = model.Lines.Select(x => new Dictionary<string, object?>
        {
            ["Date"] = model.ReportDate.ToString("yyyy-MM-dd"),
            ["CustomerCode"] = x.PartyCode,
            ["CustomerName"] = x.PartyName,
            ["RmbAmount"] = x.Amount,
            ["TxCount"] = x.TxCount
        }).ToList();

        rows.Add(new Dictionary<string, object?>
        {
            ["Date"] = model.ReportDate.ToString("yyyy-MM-dd"),
            ["CustomerCode"] = "TOTAL",
            ["CustomerName"] = "",
            ["RmbAmount"] = model.TotalRmb,
            ["TxCount"] = model.TotalCount
        });

        return SaveAsBytes(rows);
    }

    public byte[] ExportDailyBdtCollection(DailyBdtCollectionViewModel model)
    {
        var rows = model.Lines.Select(x => new Dictionary<string, object?>
        {
            ["Date"] = model.ReportDate.ToString("yyyy-MM-dd"),
            ["CustomerCode"] = x.PartyCode,
            ["CustomerName"] = x.PartyName,
            ["PaymentMode"] = x.PaymentModeName,
            ["BdtAmount"] = x.Amount,
            ["TxCount"] = x.TxCount
        }).ToList();

        rows.Add(new Dictionary<string, object?>
        {
            ["Date"] = model.ReportDate.ToString("yyyy-MM-dd"),
            ["CustomerCode"] = "TOTAL",
            ["CustomerName"] = "",
            ["PaymentMode"] = "",
            ["BdtAmount"] = model.TotalBdt,
            ["TxCount"] = null
        });

        return SaveAsBytes(rows);
    }

    public byte[] ExportProfitAnalysis(ProfitAnalysisViewModel model)
    {
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Section"] = "Summary", ["Field"] = "Period", ["Value"] = model.DateFrom.ToString("yyyy-MM-dd") + " to " + model.DateTo.ToString("yyyy-MM-dd") },
            new() { ["Section"] = "Summary", ["Field"] = "Avg USDT Cost (BDT)", ["Value"] = model.AvgUsdtCostBdt },
            new() { ["Section"] = "Summary", ["Field"] = "Avg RMB Cost (BDT)", ["Value"] = model.AvgRmbCostBdt },
            new() { ["Section"] = "Summary", ["Field"] = "Total RMB Sold", ["Value"] = model.TotalRmbSold },
            new() { ["Section"] = "Summary", ["Field"] = "Est. Gross Profit (BDT)", ["Value"] = model.EstimatedGrossProfitBdt }
        };

        foreach (var line in model.SellLines)
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["Section"] = "Sell",
                ["Date"] = line.TransDate.ToString("yyyy-MM-dd HH:mm"),
                ["Customer"] = line.PartyCode,
                ["RmbAmount"] = line.RmbAmount,
                ["SellRate"] = line.SellRate,
                ["BdtReceivable"] = line.BdtReceivable,
                ["EstCostPerRmb"] = line.EstCostBdtPerRmb,
                ["EstGrossProfit"] = line.EstGrossProfitBdt
            });
        }

        return SaveAsBytes(rows);
    }

    public byte[] ExportTransactions(TransactionListViewModel model)
    {
        var rows = model.Items.Select(x => new Dictionary<string, object?>
        {
            ["TransNo"] = x.TransNo,
            ["Date"] = x.TransDate.ToString("yyyy-MM-dd HH:mm"),
            ["Type"] = x.TransType?.NameEn,
            ["Party"] = x.Party?.Code,
            ["RelatedParty"] = x.RelatedParty?.Code,
            ["Amount"] = x.Amount,
            ["Currency"] = x.Currency,
            ["Remark"] = x.Remark,
            ["Reversed"] = x.IsDeleted ? "Yes" : (x.IsAdjustment ? "Adjustment" : "")
        }).ToList();

        return SaveAsBytes(rows);
    }

    private static byte[] SaveAsBytes(IEnumerable<object> rows)
    {
        using var stream = new MemoryStream();
        stream.SaveAs(rows);
        return stream.ToArray();
    }
}
