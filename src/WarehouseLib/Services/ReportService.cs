using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class ReportService
{
    public async Task<CategoryReport> GenerateCategoryReportAsync(
        IReadOnlyList<(Product Product, StockItem Stock)> stockData)
    {
        if (stockData == null) throw new ArgumentNullException(nameof(stockData));

        var grouped = stockData
            .GroupBy(x => x.Product.Category)
            .Select(g => new
            {
                Category = g.Key,
                Items = g.ToList()
            })
            .ToList();

        var reportLines = new List<CategoryReportLine>();
        var reportLock = new object();

        await Parallel.ForEachAsync(
            grouped,
            new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
            async (group, ct) =>
            {
                await Task.Delay(50, ct);

                var line = new CategoryReportLine
                {
                    Category = group.Category,
                    ProductCount = group.Items.Count,
                    TotalQuantity = group.Items.Sum(x => x.Stock.Quantity),
                    TotalValue = group.Items.Sum(x => x.Stock.Quantity * x.Product.Price)
                };

                lock (reportLock)
                {
                    reportLines.Add(line);
                }
            });

        reportLines.Sort((a, b) => string.Compare(a.Category, b.Category, StringComparison.Ordinal));

        return new CategoryReport
        {
            GeneratedAt = DateTime.UtcNow,
            Lines = reportLines
        };
    }
}