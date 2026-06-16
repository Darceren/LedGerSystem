using System.Security.Claims;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LedGerSystem.Controllers;

[Authorize]
public class EntryController(
    ILookupService lookupService,
    ITransactionService transactionService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Create(int? typeId = null)
    {
        var model = await BuildViewModelAsync(new TransactionEntryViewModel
        {
            TransDate = DateTime.Now,
            TransTypeId = typeId ?? 4
        });

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "Entry")] TransactionEntryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var vm = await BuildViewModelAsync(model);
            return View(vm);
        }

        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge(AppConstants.AuthScheme);
        }

        var (success, error, transaction) = await transactionService.CreateAsync(model, userId.Value);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to save transaction.");
            var vm = await BuildViewModelAsync(model);
            return View(vm);
        }

        TempData["SuccessMessage"] = $"Saved {transaction?.TransNo} successfully.";

        if (model.SaveAndAddAnother)
        {
            return RedirectToAction(nameof(Create), new { typeId = model.TransTypeId });
        }

        return RedirectToAction(nameof(Success), new { id = transaction!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Success(long id)
    {
        var saved = await transactionService.GetByIdAsync(id);
        var recent = await transactionService.GetRecentAsync(10);
        ViewBag.SavedTransaction = saved;
        return View(recent);
    }

    private async Task<TransactionEntryPageModel> BuildViewModelAsync(TransactionEntryViewModel model)
    {
        var lookups = await lookupService.GetEntryLookupsAsync();

        ViewBag.HasBanks = lookups.BankAccounts.Count > 0;

        return new TransactionEntryPageModel
        {
            Entry = model,
            TypeRulesJson = EntryTypeRules.ToJson(),
            TransactionTypes = lookups.TransactionTypes
                .Select(x => new SelectListItem($"{x.NameEn}", x.Id.ToString(), x.Id == model.TransTypeId))
                .ToList(),
            Customers = BuildPartyList(lookups.Customers, model.PartyId),
            Suppliers = BuildPartyList(lookups.Suppliers, model.RelatedPartyId ?? model.PartyId),
            ShufenParties = BuildPartyList(lookups.ShufenParties, model.PartyId),
            BankAccounts = lookups.BankAccounts
                .Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == model.BankAccountId))
                .ToList()
        };
    }

    private static List<SelectListItem> BuildPartyList(List<Entities.Party> parties, long? selectedId)
    {
        var items = new List<SelectListItem> { new("-- Select --", "") };
        items.AddRange(parties.Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == selectedId)));
        return items;
    }

    private long? GetUserId()
    {
        var value = User.FindFirstValue(AppConstants.ClaimUserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }
}
