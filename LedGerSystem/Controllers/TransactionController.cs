using System.Security.Claims;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SqlSugar;

namespace LedGerSystem.Controllers;

[Authorize]
public class TransactionController(
    ITransactionService transactionService,
    ILookupService lookupService,
    IExcelExportService excelExportService,
    ISqlSugarClient db) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(TransactionQueryModel? query)
    {
        query ??= new TransactionQueryModel();
        var model = await transactionService.GetListAsync(query);
        await PopulateFiltersAsync(model.Query);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var tx = await transactionService.GetByIdAsync(id);
        if (tx is null)
        {
            return NotFound();
        }

        var reversalIds = await db.Queryable<Entities.LedgerTransaction>()
            .Where(x => !x.IsDeleted && x.ReversedTransId == id)
            .Select(x => x.Id)
            .ToListAsync();
        var reversalId = reversalIds.FirstOrDefault();

        var model = new TransactionDetailViewModel
        {
            Transaction = tx,
            CanReverse = !tx.IsDeleted && !tx.IsAdjustment && reversalId == 0,
            ReversalId = reversalId > 0 ? reversalId : null
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Reverse(long id)
    {
        var tx = await transactionService.GetByIdAsync(id);
        if (tx is null || tx.IsDeleted)
        {
            return NotFound();
        }

        return View(new TransactionReverseViewModel
        {
            Id = tx.Id,
            TransNo = tx.TransNo,
            TypeName = tx.TransType?.NameEn ?? "-"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reverse(TransactionReverseViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge(AppConstants.AuthScheme);
        }

        var (success, error, _) = await transactionService.CreateReversalAsync(model.Id, model.Remark, userId.Value);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to reverse.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Transaction {model.TransNo} reversed.";
        return RedirectToAction(nameof(Detail), new { id = model.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Export(TransactionQueryModel query)
    {
        query.Page = 1;
        query.PageSize = 10000;
        var model = await transactionService.GetListAsync(query);
        var bytes = excelExportService.ExportTransactions(model);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Transactions_{DateTime.Today:yyyy-MM-dd}.xlsx");
    }

    private async Task PopulateFiltersAsync(TransactionQueryModel query)
    {
        var lookups = await lookupService.GetEntryLookupsAsync();
        var allTypes = await db.Queryable<Entities.SysTransactionType>()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();

        ViewBag.TransactionTypes = allTypes
            .Select(x => new SelectListItem(x.NameEn, x.Id.ToString(), x.Id == query.TransTypeId))
            .Prepend(new SelectListItem("All types", ""))
            .ToList();

        ViewBag.Parties = lookups.Customers
            .Concat(lookups.Suppliers)
            .Concat(lookups.ShufenParties)
            .DistinctBy(x => x.Id)
            .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == query.PartyId))
            .Prepend(new SelectListItem("All parties", ""))
            .ToList();
    }

    private long? GetUserId()
    {
        var value = User.FindFirstValue(AppConstants.ClaimUserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }
}
