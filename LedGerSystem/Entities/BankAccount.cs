using SqlSugar;

namespace LedGerSystem.Entities;

[SugarTable("BankAccount")]
public class BankAccount
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? BankName { get; set; }

    public string? AccountNo { get; set; }

    public string Currency { get; set; } = "BDT";

    public byte FinalAccountSide { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
