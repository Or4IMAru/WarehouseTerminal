namespace WarehouseLib.Models;

public enum StockOperationType
{
    Receive,
    Issue
}

public sealed class StockOperation
{
    public StockOperationType Type { get; init; }
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }

    public TaskCompletionSource<object?> CompletionSource { get; init; } = new();
}