using System.Globalization;
using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class LegacyInvoiceAdapter : IInvoiceImporter
{
    private readonly LegacyInvoiceParser _parser = new();

    public async Task<Invoice> ImportAsync(string filePath)
    {
        var legacy = await _parser.ParseAsync(filePath);

        var invoice = new Invoice
        {
            Number = legacy.SourceFileName,

            Date = ParseLegacyDate(legacy.DateRaw)
        };

        foreach (var item in legacy.Items)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                ProductId = item.ProductCode,
                ProductName = item.ProductName,
                Category = item.Category,
                Price = item.Price,
                Quantity = item.Quantity
            });
        }

        return invoice;
    }

    private static DateTime ParseLegacyDate(string raw)
    {
        if (!DateTime.TryParseExact(
                raw,
                "yyyy/MM/dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw new InvalidDataException($"Некорректная дата в старом формате: '{raw}'");
        }
        return date;
    }
}