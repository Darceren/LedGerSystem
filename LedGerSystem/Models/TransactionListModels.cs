using System.ComponentModel.DataAnnotations;
using LedGerSystem.Entities;

namespace LedGerSystem.Models;

public class TransactionQueryModel
{
    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int? TransTypeId { get; set; }

    public long? PartyId { get; set; }

    public string? TransNo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}

public class TransactionListViewModel
{
    public TransactionQueryModel Query { get; set; } = new();

    public List<LedgerTransaction> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int TotalPages => Query.PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)Query.PageSize) : 0;
}

public class TransactionDetailViewModel
{
    public LedgerTransaction Transaction { get; set; } = null!;

    public bool CanReverse { get; set; }

    public long? ReversalId { get; set; }
}

public class TransactionReverseViewModel
{
    public long Id { get; set; }

    public string? TransNo { get; set; }

    public string TypeName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Reason for reversal")]
    public string Remark { get; set; } = string.Empty;
}
