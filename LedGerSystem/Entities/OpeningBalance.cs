using SqlSugar;

namespace LedGerSystem.Entities;

[SugarTable("OpeningBalance")]
public class OpeningBalance
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public DateTime BalanceDate { get; set; }

    public byte EntityType { get; set; }

    public long? PartyId { get; set; }

    public int? BankAccountId { get; set; }

    public string Currency { get; set; } = "BDT";

    public decimal Amount { get; set; }

    public string? Remark { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
