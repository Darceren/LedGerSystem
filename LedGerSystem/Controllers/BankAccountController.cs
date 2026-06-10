using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedGerSystem.Controllers;

[Authorize]
public class BankAccountController(IBankAccountService bankAccountService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var list = await bankAccountService.GetListAsync(includeInactive: true);
        return View(list);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BankAccountEditViewModel { IsActive = true, FinalAccountSide = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BankAccountEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await bankAccountService.SaveAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Save failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Bank account saved successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var bank = await bankAccountService.GetByIdAsync(id);
        if (bank is null)
        {
            return NotFound();
        }

        return View(new BankAccountEditViewModel
        {
            Id = bank.Id,
            Code = bank.Code,
            Name = bank.Name,
            BankName = bank.BankName,
            AccountNo = bank.AccountNo,
            Currency = bank.Currency,
            FinalAccountSide = bank.FinalAccountSide,
            IsActive = bank.IsActive,
            SortOrder = bank.SortOrder,
            Remark = bank.Remark
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BankAccountEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await bankAccountService.SaveAsync(model);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Save failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = "Bank account updated successfully.";
        return RedirectToAction(nameof(Index));
    }
}
