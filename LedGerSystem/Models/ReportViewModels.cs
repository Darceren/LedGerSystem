namespace LedGerSystem.Models;

public class DailyRmbPayoutViewModel
{
    public DateTime ReportDate { get; set; }

    public List<DailyPartyAmountLine> Lines { get; set; } = [];

    public decimal TotalRmb { get; set; }

    public int TotalCount { get; set; }
}

public class DailyBdtCollectionViewModel
{
    public DateTime ReportDate { get; set; }

    public List<DailyBdtCollectionLine> Lines { get; set; } = [];

    public decimal TotalBdt { get; set; }
}

public class DailyPartyAmountLine
{
    public string PartyCode { get; set; } = string.Empty;

    public string PartyName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int TxCount { get; set; }

    public string Currency { get; set; } = "CNY";
}

public class DailyBdtCollectionLine
{
    public string PartyCode { get; set; } = string.Empty;

    public string PartyName { get; set; } = string.Empty;

    public string PaymentModeName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public int TxCount { get; set; }
}

public class ProfitAnalysisViewModel
{
    public DateTime DateFrom { get; set; }

    public DateTime DateTo { get; set; }

    public decimal TotalUsdtBought { get; set; }

    public decimal TotalBdtPaidForUsdt { get; set; }

    public decimal AvgUsdtCostBdt { get; set; }

    public decimal TotalRmbFromShufen { get; set; }

    public decimal AvgRmbCostBdt { get; set; }

    public decimal TotalRmbSold { get; set; }

    public decimal TotalBdtReceivableFromSales { get; set; }

    public decimal AvgSellRateBdtPerRmb { get; set; }

    public decimal EstimatedGrossProfitBdt { get; set; }

    public List<ProfitAnalysisLine> SellLines { get; set; } = [];
}

public class ProfitAnalysisLine
{
    public DateTime TransDate { get; set; }

    public string PartyCode { get; set; } = string.Empty;

    public decimal RmbAmount { get; set; }

    public decimal SellRate { get; set; }

    public decimal BdtReceivable { get; set; }

    public decimal EstCostBdtPerRmb { get; set; }

    public decimal EstGrossProfitBdt { get; set; }
}
