using LedGerSystem.Infrastructure;
using LedGerSystem.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLedGerSqlSugar(builder.Configuration);
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IBootstrapDataService, BootstrapDataService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();

builder.Services
    .AddAuthentication(AppConstants.AuthScheme)
    .AddCookie(AppConstants.AuthScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await authService.EnsureBootstrapUserAsync();

    var bootstrapData = scope.ServiceProvider.GetRequiredService<IBootstrapDataService>();
    await bootstrapData.EnsureSampleDataAsync();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
