using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class WarehouseService
{
    private readonly Dictionary<string, StockItem> _stock = new();
    private readonly object _lock = new();

    public Task ReceiveGoodsAsync(string productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Количество должно быть больше нуля", nameof(quantity));

        lock (_lock)
        {
            if (_stock.TryGetValue(productId, out var item))
            {
                item.Quantity += quantity;
                item.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                _stock[productId] = new StockItem
                {
                    ProductId = productId,
                    Quantity = quantity,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> IssueGoodsAsync(string productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentException("Количество должно быть больше нуля", nameof(quantity));

        lock (_lock)
        {
            if (_stock.TryGetValue(productId, out var item))
            {
                if (item.Quantity >= quantity)
                {
                    item.Quantity -= quantity;
                    item.LastUpdated = DateTime.UtcNow;
                    return Task.FromResult(true);
                }
            }
            return Task.FromResult(false);
        }
    }

    public Task<StockItem?> GetStockAsync(string productId)
    {
        lock (_lock)
        {
            return Task.FromResult(_stock.TryGetValue(productId, out var item) ? item : null);
        }
    }

    public Task<int> ApplyInvoiceAsync(Invoice invoice)
    {
        if (invoice == null) throw new ArgumentNullException(nameof(invoice));

        int totalReceived = 0;
        lock (_lock)
        {
            foreach (var line in invoice.Lines)
            {
                if (_stock.TryGetValue(line.ProductId, out var item))
                {
                    item.Quantity += line.Quantity;
                    item.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    _stock[line.ProductId] = new StockItem
                    {
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                }
                totalReceived += line.Quantity;
            }
        }
        return Task.FromResult(totalReceived);
    }
}