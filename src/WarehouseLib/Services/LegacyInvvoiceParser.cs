using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class LegacyInvoiceParser
{
    public async Task<LegacyInvoiceData> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл старой накладной не найден", filePath);

        var data = new LegacyInvoiceData
        {
            SourceFileName = Path.GetFileNameWithoutExtension(filePath)
        };

        using var reader = new StreamReader(filePath);
        string? line;
        int lineNumber = 0;
        int expectedItems = -1;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("SUPPLIER:", StringComparison.OrdinalIgnoreCase))
            {
                data.SupplierName = line["SUPPLIER:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("DATE:", StringComparison.OrdinalIgnoreCase))
            {
                data.DateRaw = line["DATE:".Length..].Trim();
                continue;
            }

            if (line.StartsWith("ITEMS_COUNT:", StringComparison.OrdinalIgnoreCase))
            {
                var countStr = line["ITEMS_COUNT:".Length..].Trim();
                if (!int.TryParse(countStr, out var count) || count < 0)
                    throw new InvalidDataException($"Строка {lineNumber}: некорректное ITEMS_COUNT '{countStr}'");
                expectedItems = count;
                continue;
            }

            var parts = line.Split('|');
            if (parts.Length != 5)
                throw new InvalidDataException($"Строка {lineNumber}: ожидается 5 полей, получено {parts.Length}");

            if (!int.TryParse(parts[2], out var quantity) || quantity < 0)
                throw new InvalidDataException($"Строка {lineNumber}: некорректное количество '{parts[2]}'");

            if (!decimal.TryParse(parts[3], out var price) || price < 0)
                throw new InvalidDataException($"Строка {lineNumber}: некорректная цена '{parts[3]}'");

            data.Items.Add(new LegacyInvoiceItem
            {
                ProductName = parts[0].Trim(),
                ProductCode = parts[1].Trim(),
                Quantity = quantity,
                Price = price,
                Category = parts[4].Trim()
            });
        }

        if (expectedItems >= 0 && data.Items.Count != expectedItems)
            throw new InvalidDataException(
                $"Ожидалось {expectedItems} позиций, фактически прочитано {data.Items.Count}");

        return data;
    }
}