using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedGerSystem.Controllers;

[Authorize]
public class HomeController(
    ITransactionService transactionService,
    IBalanceService balanceService,
    IPartyService partyService,
    IBankAccountService bankAccountService,
    IReportService reportService,
    IOpeningBalanceService openingBalanceService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var snapshot = await balanceService.GetSnapshotAsync();
        var parties = await partyService.GetListAsync();
        var banks = await bankAccountService.GetListAsync();
        var today = DateTime.Today;

        var partyLines = parties
            .Where(p => snapshot.PartyBalances.TryGetValue(p.Id, out var b) && b != 0)
            .Select(p => new BalanceLineItem
            {
                Code = p.Code,
                Name = p.Name,
                Category = p.PartyType switch
                {
                    1 => "Customer",
                    2 => "Supplier",
                    3 => "Shufen",
                    _ => "Other"
                },
                Balance = snapshot.PartyBalances[p.Id]
            })
            .OrderByDescending(x => Math.Abs(x.Balance))
            .ToList();

        var bankLines = banks
            .Where(b => snapshot.BankBalances.TryGetValue(b.Id, out var bal) && bal != 0)
            .Select(b => new BalanceLineItem
            {
                Code = b.Code,
                Name = b.Name,
                Category = "Bank",
                Balance = snapshot.BankBalances[b.Id],
                Currency = b.Currency
            })
            .OrderByDescending(x => Math.Abs(x.Balance))
            .ToList();

        var openingPage = await openingBalanceService.GetPageAsync();

        var model = new DashboardViewModel
        {
            CashOnHand = snapshot.CashOnHand,
            TotalCustomerReceivable = partyLines.Where(x => x.Category == "Customer" && x.Balance > 0).Sum(x => x.Balance),
            TotalSupplierPayable = partyLines.Where(x => x.Category == "Supplier" && x.Balance > 0).Sum(x => x.Balance),
            TodayBdtCollected = await reportService.GetTodayBdtCollectedAsync(today),
            TodayTransactionCount = await reportService.GetTodayTransactionCountAsync(today),
            HasBankAccounts = banks.Count > 0,
            HasOpeningBalances = openingPage.Items.Count > 0,
            PartyBalances = partyLines,
            BankBalances = bankLines,
            RecentTransactions = await transactionService.GetRecentAsync(10)
        };

        return View(model);
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
