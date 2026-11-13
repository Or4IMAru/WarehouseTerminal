namespace WarehouseLib.Models;

public sealed class CategoryReportLine
{
    public string Category { get; init; } = string.Empty;
    public int ProductCount { get; init; }
    public int TotalQuantity { get; init; }
    public decimal TotalValue { get; init; }
}

public sealed class CategoryReport
{
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public List<CategoryReportLine> Lines { get; init; } = new();
    public int TotalProducts => Lines.Sum(l => l.ProductCount);
    public int TotalQuantity => Lines.Sum(l => l.TotalQuantity);
    public decimal TotalValue => Lines.Sum(l => l.TotalValue);
}