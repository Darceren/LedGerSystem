using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedGerSystem.Controllers;

[Authorize]
public class HomeController(
    ITransactionService transactionService,
    IBalanceService balanceService,
    IPartyService partyService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var snapshot = await balanceService.GetSnapshotAsync();
        var parties = await partyService.GetListAsync();

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
            .Take(8)
            .ToList();

        var model = new DashboardViewModel
        {
            CashOnHand = snapshot.CashOnHand,
            TotalCustomerReceivable = partyLines.Where(x => x.Category == "Customer" && x.Balance > 0).Sum(x => x.Balance),
            TotalSupplierPayable = partyLines.Where(x => x.Category == "Supplier" && x.Balance > 0).Sum(x => x.Balance),
            TopPartyBalances = partyLines,
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
