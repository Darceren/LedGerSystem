using System.ComponentModel.DataAnnotations;

namespace LedGerSystem.Models;

public class PartyEditViewModel
{
    public long Id { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Party Type")]
    public byte PartyType { get; set; }

    [Display(Name = "Final Account Side")]
    public byte FinalAccountSide { get; set; }

    [MaxLength(50)]
    [Display(Name = "Phone")]
    public string? ContactPhone { get; set; }

    [MaxLength(500)]
    public string? Remark { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Sort Order")]
    public int SortOrder { get; set; }
}
