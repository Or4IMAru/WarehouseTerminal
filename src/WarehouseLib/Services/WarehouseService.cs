using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class WarehouseService
{
    private readonly Dictionary<string, StockItem> _stock = new();
    private readonly Dictionary<string, Product> _products = new();
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
                _products[line.ProductId] = new Product
                {
                    Id = line.ProductId,
                    Name = line.ProductName,
                    Category = line.Category,
                    Price = line.Price
                };
            }
        }
        return Task.FromResult(totalReceived);
    }
    public Task<IReadOnlyList<(Product Product, StockItem Stock)>> GetAllStockWithProductsAsync()
    {
        lock (_lock)
        {
            var result = new List<(Product, StockItem)>();
            foreach (var (productId, stock) in _stock)
            {
                if (_products.TryGetValue(productId, out var product))
                {
                    result.Add((product, stock));
                }
            }
            return Task.FromResult<IReadOnlyList<(Product, StockItem)>>(result);
        }
    }
}