using System.Security.Claims;
using LedGerSystem.Infrastructure;
using LedGerSystem.Models;
using LedGerSystem.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LedGerSystem.Controllers;

public class AccountController(IAuthService authService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, error) = await authService.LoginAsync(model.UserName.Trim(), model.Password);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Login failed.");
            return View(model);
        }

        var user = await authService.GetByUserNameAsync(model.UserName.Trim());
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "User not found.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(AppConstants.ClaimUserId, user.Id.ToString()),
            new(AppConstants.ClaimDisplayName, user.DisplayName)
        };

        var identity = new ClaimsIdentity(claims, AppConstants.AuthScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AppConstants.AuthScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        return RedirectToLocal(model.ReturnUrl);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AppConstants.AuthScheme);
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
