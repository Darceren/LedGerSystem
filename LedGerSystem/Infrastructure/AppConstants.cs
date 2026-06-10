namespace LedGerSystem.Infrastructure;

public static class AppConstants
{
    public const string AuthScheme = "LedGerCookie";
    public const string ClaimUserId = "UserId";
    public const string ClaimDisplayName = "DisplayName";

    public const string CurrencyBdt = "BDT";
    public const string CurrencyCny = "CNY";
    public const string CurrencyUsdt = "USDT";

    public const byte PaymentModeCash = 1;
    public const byte PaymentModeMyBank = 2;
    public const byte PaymentModeSupplierBank = 3;

    public const byte PartyTypeCustomer = 1;
    public const byte PartyTypeSupplier = 2;
    public const byte PartyTypeShufen = 3;
    public const byte PartyTypeOther = 4;

    public const byte PayMethodCash = 1;
    public const byte PayMethodBank = 2;
}
