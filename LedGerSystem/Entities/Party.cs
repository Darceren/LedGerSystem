using SqlSugar;

namespace LedGerSystem.Entities;

[SugarTable("Party")]
public class Party
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public byte PartyType { get; set; }

    public byte FinalAccountSide { get; set; }

    public string? ContactPhone { get; set; }

    public string? Remark { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
