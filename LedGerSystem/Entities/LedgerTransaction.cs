using SqlSugar;

namespace LedGerSystem.Entities;

[SugarTable("LedgerTransaction")]
public class LedgerTransaction
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    public string? TransNo { get; set; }

    public DateTime TransDate { get; set; }

    [SugarColumn(IsOnlyIgnoreInsert = true, IsOnlyIgnoreUpdate = true)]
    public DateTime TransDateOnly { get; set; }

    public int TransTypeId { get; set; }

    public long? PartyId { get; set; }

    public long? RelatedPartyId { get; set; }

    public int? BankAccountId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "BDT";

    public decimal? UnitPrice { get; set; }

    public decimal? Quantity { get; set; }

    public string? QuantityCurrency { get; set; }

    public decimal? EquivalentAmount { get; set; }

    public string? EquivalentCurrency { get; set; }

    public byte? PaymentMode { get; set; }

    public byte? PayMethod { get; set; }

    public byte? PayoutChannel { get; set; }

    public string? PayoutAccountName { get; set; }

    public string? PayoutAccountNo { get; set; }

    public bool IsOpening { get; set; }

    public bool IsAdjustment { get; set; }

    public long? ReversedTransId { get; set; }

    public string? BatchNo { get; set; }

    public string? Remark { get; set; }

    public long? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(TransTypeId))]
    public SysTransactionType? TransType { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(PartyId))]
    public Party? Party { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(RelatedPartyId))]
    public Party? RelatedParty { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(BankAccountId))]
    public BankAccount? BankAccount { get; set; }
}
