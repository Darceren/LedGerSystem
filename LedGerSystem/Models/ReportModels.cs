namespace LedGerSystem.Models;

public class DashboardViewModel
{
    public decimal CashOnHand { get; set; }

    public decimal TotalCustomerReceivable { get; set; }

    public decimal TotalSupplierPayable { get; set; }

    public decimal TodayBdtCollected { get; set; }

    public int TodayTransactionCount { get; set; }

    public bool HasBankAccounts { get; set; }

    public bool HasOpeningBalances { get; set; }

    public List<BalanceLineItem> PartyBalances { get; set; } = [];

    public List<BalanceLineItem> BankBalances { get; set; } = [];

    public List<Entities.LedgerTransaction> RecentTransactions { get; set; } = [];
}

public class BalanceLineItem
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "BDT";
}

public class FinalAccountViewModel
{
    public DateTime AsOfDate { get; set; }

    public List<BalanceLineItem> CreditLines { get; set; } = [];

    public List<BalanceLineItem> DebitLines { get; set; } = [];

    public decimal TotalCredit { get; set; }

    public decimal TotalDebit { get; set; }

    public decimal TotalExpenses { get; set; }

    public decimal Profit { get; set; }
}
