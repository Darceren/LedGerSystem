using System.ComponentModel.DataAnnotations;

namespace LedGerSystem.Models;

public class TransactionEntryViewModel
{
    [Required]
    [Display(Name = "Transaction Type")]
    public int TransTypeId { get; set; }

    [Required]
    [Display(Name = "Date & Time")]
    public DateTime TransDate { get; set; } = DateTime.Now;

    [Display(Name = "Customer / Party")]
    public long? PartyId { get; set; }

    [Display(Name = "Supplier")]
    public long? RelatedPartyId { get; set; }

    [Display(Name = "Bank Account")]
    public int? BankAccountId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    [Display(Name = "Amount")]
    public decimal Amount { get; set; }

    [Display(Name = "Currency")]
    public string Currency { get; set; } = "BDT";

    [Display(Name = "Rate / Unit Price")]
    public decimal? UnitPrice { get; set; }

    [Display(Name = "Quantity")]
    public decimal? Quantity { get; set; }

    public string? QuantityCurrency { get; set; }

    [Display(Name = "Equivalent Amount")]
    public decimal? EquivalentAmount { get; set; }

    public string? EquivalentCurrency { get; set; }

    [Display(Name = "Payment Mode")]
    public byte? PaymentMode { get; set; }

    [Display(Name = "Pay Method")]
    public byte? PayMethod { get; set; }

    [Display(Name = "Payout Channel")]
    public byte? PayoutChannel { get; set; }

    [Display(Name = "Account Name")]
    public string? PayoutAccountName { get; set; }

    [Display(Name = "Account No.")]
    public string? PayoutAccountNo { get; set; }

    [Display(Name = "Remarks")]
    public string? Remark { get; set; }

    public bool SaveAndAddAnother { get; set; }
}
