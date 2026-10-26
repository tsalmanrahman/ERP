using ERP.Common.Domain.Common;

namespace ERP.Common.Domain.Entities.Inventory;

public class ProductPriceHistory : Entity<int>, IMultiTenant
{
    public int CompanyId { get; set; }
    public int ProductId { get; set; }
    public decimal OldStandardCost { get; set; }
    public decimal NewStandardCost { get; set; }
    public decimal OldSellingPrice { get; set; }
    public decimal NewSellingPrice { get; set; }
    public decimal OldMinRetailPrice { get; set; }
    public decimal NewMinRetailPrice { get; set; }
    public string? ChangeReason { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public string ChangedBy { get; set; } = "System";

    public ProductPriceHistory() { }
}
