namespace WarehouseLib.Models;

public class StockItem
{
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}