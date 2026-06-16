using System.Security.Claims;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LedGerSystem.Controllers;

[Authorize]
public class OpeningBalanceController(
    IOpeningBalanceService openingBalanceService,
    IPartyService partyService,
    IBankAccountService bankAccountService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(long? editId = null)
    {
        OpeningBalanceEditViewModel? form = null;
        if (editId.HasValue)
        {
            var entity = await openingBalanceService.GetByIdAsync(editId.Value);
            if (entity is not null)
            {
                form = new OpeningBalanceEditViewModel
                {
                    Id = entity.Id,
                    BalanceDate = entity.BalanceDate,
                    EntityType = entity.EntityType,
                    PartyId = entity.PartyId,
                    BankAccountId = entity.BankAccountId,
                    Currency = entity.Currency,
                    Amount = entity.Amount,
                    Remark = entity.Remark
                };
            }
        }

        var model = await openingBalanceService.GetPageAsync(form);
        ViewBag.Parties = await BuildPartySelectList(form?.PartyId);
        ViewBag.Banks = await BuildBankSelectList(form?.BankAccountId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(OpeningBalanceEditViewModel model)
    {
        var userId = GetUserId();
        if (userId is null)
        {
            return Challenge(AppConstants.AuthScheme);
        }

        if (!ModelState.IsValid)
        {
            var page = await openingBalanceService.GetPageAsync(model);
            ViewBag.Parties = await BuildPartySelectList(model.PartyId);
            ViewBag.Banks = await BuildBankSelectList(model.BankAccountId);
            return View("Index", page);
        }

        var (success, error) = await openingBalanceService.SaveAsync(model, userId.Value);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to save.");
            var page = await openingBalanceService.GetPageAsync(model);
            ViewBag.Parties = await BuildPartySelectList(model.PartyId);
            ViewBag.Banks = await BuildBankSelectList(model.BankAccountId);
            return View("Index", page);
        }

        TempData["SuccessMessage"] = model.Id.HasValue ? "Opening balance updated." : "Opening balance saved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var (success, error) = await openingBalanceService.DeleteAsync(id);
        TempData[success ? "SuccessMessage" : "ErrorMessage"] = success ? "Opening balance deleted." : error;
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> BuildPartySelectList(long? selectedId)
    {
        var parties = await partyService.GetListAsync();
        var items = new List<SelectListItem> { new("-- Select Party --", "") };
        items.AddRange(parties.Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == selectedId)));
        return items;
    }

    private async Task<List<SelectListItem>> BuildBankSelectList(int? selectedId)
    {
        var banks = await bankAccountService.GetListAsync();
        var items = new List<SelectListItem> { new("-- Select Bank --", "") };
        items.AddRange(banks.Select(x => new SelectListItem($"{x.Code} - {x.Name}", x.Id.ToString(), x.Id == selectedId)));
        return items;
    }

    private long? GetUserId()
    {
        var value = User.FindFirstValue(AppConstants.ClaimUserId)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var id) ? id : null;
    }
}
