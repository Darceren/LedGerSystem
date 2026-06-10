using SqlSugar;

namespace LedGerSystem.Entities;

[SugarTable("Sys_TransactionType")]
public class SysTransactionType
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string? NameZh { get; set; }

    public byte Category { get; set; }

    public bool AffectsCash { get; set; }

    public bool AffectsBank { get; set; }

    public bool AffectsPartyBalance { get; set; }

    public bool RequireParty { get; set; }

    public bool RequireRelatedParty { get; set; }

    public bool RequireBankAccount { get; set; }

    public bool RequirePaymentMode { get; set; }

    public bool RequirePayoutChannel { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Remark { get; set; }
}
