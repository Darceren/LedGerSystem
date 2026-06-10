using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LedGerSystem.Controllers;

[Authorize]
public class PartyController(IPartyService partyService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(byte? type = null)
    {
        ViewBag.PartyType = type;
        var list = await partyService.GetListAsync(type, includeInactive: true);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create(byte? type = null)
    {
        return View(BuildEditModel(new PartyEditViewModel
        {
            PartyType = type ?? AppConstants.PartyTypeCustomer,
            IsActive = true
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PartyEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildEditModel(model));
        }

        var (success, error) = await partyService.SaveAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Save failed.");
            return View(BuildEditModel(model));
        }

        TempData["SuccessMessage"] = "Party saved successfully.";
        return RedirectToAction(nameof(Index), new { type = model.PartyType });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var party = await partyService.GetByIdAsync(id);
        if (party is null)
        {
            return NotFound();
        }

        return View(BuildEditModel(new PartyEditViewModel
        {
            Id = party.Id,
            Code = party.Code,
            Name = party.Name,
            PartyType = party.PartyType,
            FinalAccountSide = party.FinalAccountSide,
            ContactPhone = party.ContactPhone,
            Remark = party.Remark,
            IsActive = party.IsActive,
            SortOrder = party.SortOrder
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PartyEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(BuildEditModel(model));
        }

        var (success, error) = await partyService.SaveAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Save failed.");
            return View(BuildEditModel(model));
        }

        TempData["SuccessMessage"] = "Party updated successfully.";
        return RedirectToAction(nameof(Index), new { type = model.PartyType });
    }

    private static PartyEditViewModel BuildEditModel(PartyEditViewModel model)
    {
        return model;
    }
}

public static class PartySelectLists
{
    public static List<SelectListItem> PartyTypes() =>
    [
        new("Customer", "1"),
        new("USDT Supplier", "2"),
        new("Shufen", "3"),
        new("Other", "4")
    ];

    public static List<SelectListItem> FinalAccountSides() =>
    [
        new("Not in Final Account", "0"),
        new("Credit (Receivable/Asset)", "1"),
        new("Debit (Payable/Liability)", "2")
    ];
}
