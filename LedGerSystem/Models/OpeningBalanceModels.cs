using System.ComponentModel.DataAnnotations;

namespace LedGerSystem.Models;

public class OpeningBalanceListItem
{
    public long Id { get; set; }

    public DateTime BalanceDate { get; set; }

    public byte EntityType { get; set; }

    public string EntityTypeName { get; set; } = string.Empty;

    public string EntityCode { get; set; } = string.Empty;

    public string EntityName { get; set; } = string.Empty;

    public string Currency { get; set; } = "BDT";

    public decimal Amount { get; set; }

    public string? Remark { get; set; }
}

public class OpeningBalanceEditViewModel
{
    public long? Id { get; set; }

    [Required]
    [Display(Name = "Effective Date")]
    [DataType(DataType.Date)]
    public DateTime BalanceDate { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "Entity Type")]
    public byte EntityType { get; set; } = 1;

    [Display(Name = "Party")]
    public long? PartyId { get; set; }

    [Display(Name = "Bank Account")]
    public int? BankAccountId { get; set; }

    [Required]
    [Display(Name = "Currency")]
    public string Currency { get; set; } = "BDT";

    [Required]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Display(Name = "Remark")]
    public string? Remark { get; set; }
}

public class OpeningBalancePageModel
{
    public List<OpeningBalanceListItem> Items { get; set; } = [];

    public OpeningBalanceEditViewModel Form { get; set; } = new();
}
