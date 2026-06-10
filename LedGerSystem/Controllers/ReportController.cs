using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedGerSystem.Controllers;

[Authorize]
public class ReportController(IBalanceService balanceService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> FinalAccount(DateTime? date = null)
    {
        var asOfDate = date?.Date ?? DateTime.Today;
        var model = await balanceService.GetFinalAccountAsync(asOfDate);
        return View(model);
    }
}
