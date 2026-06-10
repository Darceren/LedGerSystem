using Microsoft.AspNetCore.Mvc.Rendering;

namespace LedGerSystem.Models;

public class TransactionEntryPageModel
{
    public TransactionEntryViewModel Entry { get; set; } = new();

    public List<SelectListItem> TransactionTypes { get; set; } = [];

    public List<SelectListItem> Customers { get; set; } = [];

    public List<SelectListItem> Suppliers { get; set; } = [];

    public List<SelectListItem> ShufenParties { get; set; } = [];

    public List<SelectListItem> BankAccounts { get; set; } = [];

    /// <summary>JSON map: typeId -> field rules for client script.</summary>
    public string TypeRulesJson { get; set; } = "{}";
}
