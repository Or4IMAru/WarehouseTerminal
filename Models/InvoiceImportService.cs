using Variant10.Models;

namespace Variant10.Services;

public sealed class InvoiceImportService
{
    public async Task<Invoice> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Файл накладной не найден", filePath);

        var invoice = new Invoice();

        using var reader = new StreamReader(filePath);
        string? lineNumber = await reader.ReadLineAsync();
        if (lineNumber == null)
            throw new InvalidDataException("Файл пуст или не содержит заголовок");

        var header = lineNumber.Split(';');
        if (header.Length < 2)
            throw new InvalidDataException("Некорректный заголовок накладной");

        invoice.Number = header[0];
        if (!DateTime.TryParse(header[1], out var date))
            throw new InvalidDataException($"Некорректная дата накладной: {header[1]}");
        invoice.Date = date;

        string? line;
        int lineNumberIndex = 1;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineNumberIndex++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(';');
            if (parts.Length != 5)
                throw new InvalidDataException($"Строка {lineNumberIndex}: ожидается 5 полей, получено {parts.Length}");

            if (!int.TryParse(parts[4], out var quantity) || quantity < 0)
                throw new InvalidDataException($"Строка {lineNumberIndex}: некорректное количество '{parts[4]}'");

            if (!decimal.TryParse(parts[3], out var price) || price < 0)
                throw new InvalidDataException($"Строка {lineNumberIndex}: некорректная цена '{parts[3]}'");

            invoice.Lines.Add(new InvoiceLine
            {
                ProductId = parts[0],
                ProductName = parts[1],
                Category = parts[2],
                Price = price,
                Quantity = quantity
            });
        }

        return invoice;
    }
}