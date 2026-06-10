using System.ComponentModel.DataAnnotations;

namespace LedGerSystem.Models;

public class BankAccountEditViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    [Display(Name = "Bank Name")]
    public string? BankName { get; set; }

    [MaxLength(100)]
    [Display(Name = "Account No.")]
    public string? AccountNo { get; set; }

    [Required]
    public string Currency { get; set; } = "BDT";

    [Display(Name = "Final Account Side")]
    public byte FinalAccountSide { get; set; } = 1;

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }
}
