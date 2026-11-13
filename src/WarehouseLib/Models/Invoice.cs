namespace WarehouseLib.Models;

public class InvoiceLine
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class Invoice
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public List<InvoiceLine> Lines { get; set; } = new();
}