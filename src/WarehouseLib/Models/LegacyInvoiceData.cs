namespace WarehouseLib.Models;

public sealed class LegacyInvoiceItem
{
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class LegacyInvoiceData
{
    public string SupplierName { get; set; } = string.Empty;
    public string DateRaw { get; set; } = string.Empty;
    public string SourceFileName { get; set; } = string.Empty;
    public List<LegacyInvoiceItem> Items { get; set; } = new();
}