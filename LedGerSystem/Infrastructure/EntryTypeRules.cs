using System.Text.Json;

namespace LedGerSystem.Infrastructure;

/// <summary>Client-side field visibility rules per transaction type id.</summary>
public static class EntryTypeRules
{
    public static string ToJson() => JsonSerializer.Serialize(Rules);

    public static readonly Dictionary<int, EntryTypeRule> Rules = new()
    {
        [1] = new("BUY_USDT", "Supplier", "BDT", true, false, false, true, false, true, false, false),
        [2] = new("USDT_TO_RMB", "Shufen", "USDT", true, false, false, false, false, false, true, true),
        [3] = new("SELL_RMB", "Customer", "CNY", true, false, false, false, true, false, false, true),
        [4] = new("COLLECT_BDT_CASH", "Customer", "BDT", true, false, false, false, false, false, false, false),
        [5] = new("COLLECT_BDT_BANK", "Customer", "BDT", true, true, false, false, false, false, false, false),
        [6] = new("COLLECT_BDT_SUPPLIER", "Customer", "BDT", true, false, true, false, false, false, false, false),
        [7] = new("PAY_BDT_SUPPLIER", "Supplier", "BDT", true, false, false, true, false, true, false, false),
        [8] = new("EXPENSE", "None", "BDT", false, true, false, true, false, true, false, false)
    };
}

public record EntryTypeRule(
    string Code,
    string PartyRole,
    string Currency,
    bool ShowParty,
    bool ShowBank,
    bool ShowSupplier,
    bool ShowPayMethod,
    bool ShowPayout,
    bool ShowRate,
    bool ShowQuantity,
    bool ShowEquivalent);
