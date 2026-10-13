using InteAcc.Common.Domain.Common;

namespace InteAcc.Common.Domain.Entities.Migration;

public class CutoverAudit : Entity<int>, IMultiTenant, IAuditableEntity, ISoftDeletable
{
    public int CompanyId { get; set; }
    public DateTime CutoverDate { get; set; } = DateTime.UtcNow;
    public string SignedOffBy { get; set; } = string.Empty;
    public string CfoApprovalCode { get; set; } = string.Empty;
    public decimal TrialBalanceVariance { get; set; }
    public decimal ArSubledgerVariance { get; set; }
    public decimal ApSubledgerVariance { get; set; }
    public decimal InventorySubledgerVariance { get; set; }
    public decimal BankSubledgerVariance { get; set; }
    public bool IsParityVerified { get; set; }
    public string CutoverStatus { get; set; } = "Approved";
    public string? Notes { get; set; }

    // Audit and Soft Delete
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; } = "SYSTEM";
    public DateTime? ModifiedAtUtc { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
