using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using WarehouseLib.Models;
using WarehouseLib.Services;
using Variant10.Services;

namespace Variant10;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Зачётный проект 10: Складской терминал");
        Console.WriteLine(new string('=', 64));

        // Стартовый сценарий нужен для проверки структуры проекта.
        // Реализацию индивидуальных требований выполняйте в ProjectService.
        var service = new ProjectService();
        await service.RunAsync();

        Console.WriteLine();
        Console.WriteLine("Нажмите Enter для завершения.");
        Console.ReadLine();
    }
}

public sealed class ProjectService
{
    private readonly WarehouseService warehouseService = new();
    private readonly InvoiceImportService invoiceImportService = new();
    private readonly ValidatorService validatorService = new();
    public async Task RunAsync()
    {
        // TODO: перенесите предметную логику в отдельные методы и классы.
        // TODO: не размещайте весь проект в Main.
        // TODO 1: Создать модель товара и складского остатка. -- Product.cs, StockIlem.cs
        var sampleProduct = new Product
        {
            Id = "PROD-001",
            Name = "Смартфон",
            Category = "Электроника",
            Price = 29990.00m
        };

        var sampleStock = new StockItem
        {
            ProductId = sampleProduct.Id,
            Quantity = 50,
            LastUpdated = DateTime.UtcNow
        };

        Console.WriteLine($"Товар: {sampleProduct.Name}, Категория: {sampleProduct.Category}, Остаток: {sampleStock.Quantity} шт.");

        // TODO 2: Реализовать приход и расход товара. -- WarehouseService.cs
        Console.WriteLine("\nТестирование прихода и расхода товара:");

        string productId = "PROD-001";

        await warehouseService.ReceiveGoodsAsync(productId, 100);
        var stock1 = await warehouseService.GetStockAsync(productId);
        Console.WriteLine($"  -> Приход 100 шт. Текущий остаток: {stock1?.Quantity} шт.");

        bool success = await warehouseService.IssueGoodsAsync(productId, 30);
        var stock2 = await warehouseService.GetStockAsync(productId);
        Console.WriteLine($"  -> Расход 30 шт. Успешно: {success}. Текущий остаток: {stock2?.Quantity} шт.");

        bool failSuccess = await warehouseService.IssueGoodsAsync(productId, 200);
        var stock3 = await warehouseService.GetStockAsync(productId);
        Console.WriteLine($"  -> Попытка расхода 200 шт. Успешно: {failSuccess}. Текущий остаток: {stock3?.Quantity} шт. (не изменился)");

        // TODO 3: Обработать несколько операций одновременно без потери обновлений.
        Console.WriteLine("\nСтресс-тест параллельных операций:");
        await StressTestAsync(productId);

        // TODO 4: Добавить импорт накладной из текстового файла асинхронно.
        Console.WriteLine("\n Асинхронный импорт накладной из файла:");
        const string invoicePath = "invoice_sample.txt";
        await ImportInvoiceDemoAsync(invoicePath);

        // TODO 5: Запустить внешнюю программу-валидатор через Process.-- ValidatorService.cs
        bool isValid = await validatorService.ValidateInvoiceAsync(invoicePath);
        Console.WriteLine($"  -> Результат валидации: {(isValid ? "✓ Файл корректен" : "✗ Файл не прошёл проверку")}");

        const string invalidInvoicePath = "invalid_invoice.txt";
        await File.WriteAllTextAsync(invalidInvoicePath, "NOT_AN_INVOICE\nSome data");
        bool isInvalid = await validatorService.ValidateInvoiceAsync(invalidInvoicePath);
        Console.WriteLine($"  -> Результат валидации некорректного файла: {(isInvalid ? "✓ Файл корректен" : "✗ Файл не прошёл проверку (ожидаемо)")}");
        File.Delete(invalidInvoicePath);
        // TODO 6: Вынести складскую логику в DLL и подключить её через ProjectReference.
        Console.WriteLine("\n Складская логика вынесена в DLL WarehouseLib.");
        Console.WriteLine("  -> Подключена через ProjectReference.");
        Console.WriteLine("  -> Namespace: WarehouseLib.Models / WarehouseLib.Services.");
        // TODO 7: Сохранять настройки склада в HKCU.


        // TODO 8: Добавить адаптер для старого формата накладной.
        // TODO 9: Реализовать очередь операций через Channel.
        // TODO 10: Сформировать параллельный отчёт по категориям.

        Console.WriteLine("Стартовый проект собран. Реализуйте задания варианта.");
        await Task.CompletedTask;
        if (File.Exists(invoicePath))
            File.Delete(invoicePath);
    }
    private async Task StressTestAsync(string productId)
    {
        var current = await warehouseService.GetStockAsync(productId);
        if (current != null)
        {
            await warehouseService.IssueGoodsAsync(productId, current.Quantity);
        }

        const int receiveCount = 1000;
        var receiveTasks = new Task[receiveCount];
        for (int i = 0; i < receiveCount; i++)
        {
            receiveTasks[i] = warehouseService.ReceiveGoodsAsync(productId, 1);
        }
        await Task.WhenAll(receiveTasks);

        var stockAfterReceive = await warehouseService.GetStockAsync(productId);
        Console.WriteLine($"  -> Параллельный приход {receiveCount} раз по 1 шт. " +
                          $"Ожидалось: {receiveCount}, получено: {stockAfterReceive?.Quantity}. " +
                          $"Результат: {(stockAfterReceive?.Quantity == receiveCount ? "✓ ОК" : "✗ ПОТЕРЯ ДАННЫХ")}");
        const int issueAttempts = 1500;
        var issueTasks = new Task<bool>[issueAttempts];
        for (int i = 0; i < issueAttempts; i++)
        {
            issueTasks[i] = warehouseService.IssueGoodsAsync(productId, 1);
        }
        var results = await Task.WhenAll(issueTasks);

        int successCount = results.Count(r => r);
        int failCount = results.Length - successCount;

        var stockAfterIssue = await warehouseService.GetStockAsync(productId);
        Console.WriteLine($"  -> Параллельный расход: {issueAttempts} попыток. " +
                          $"Успешных: {successCount}, отказов: {failCount}. " +
                          $"Остаток: {stockAfterIssue?.Quantity}.");
        Console.WriteLine($"  -> Проверка: {(successCount == 1000 && failCount == 500 && stockAfterIssue?.Quantity == 0 ? "✓ ОК" : "✗ ОШИБКА СИНХРОНИЗАЦИИ")}");
    }
    private async Task ImportInvoiceDemoAsync(string invoicePath)
    {
        var invoiceContent =
            "INV-2026-001;2026-11-13\n" +
            "PROD-100;Клавиатура;Периферия;2500.00;50\n" +
            "PROD-101;Мышь;Периферия;800.00;120\n" +
            "PROD-102;Монитор;Электроника;15000.00;15\n";

        await File.WriteAllTextAsync(invoicePath, invoiceContent);
        Console.WriteLine($"  -> Создан тестовый файл: {invoicePath}");

        try
        {
            var invoice = await invoiceImportService.ImportAsync(invoicePath);
            Console.WriteLine($"  -> Импортирована накладная №{invoice.Number} от {invoice.Date:yyyy-MM-dd}, позиций: {invoice.Lines.Count}");

            foreach (var line in invoice.Lines)
            {
                Console.WriteLine($"     • {line.ProductName} ({line.ProductId}): {line.Quantity} шт. × {line.Price} ₽");
            }

            int totalReceived = await warehouseService.ApplyInvoiceAsync(invoice);
            Console.WriteLine($"  -> Оприходовано всего: {totalReceived} шт.");

            foreach (var line in invoice.Lines)
            {
                var stock = await warehouseService.GetStockAsync(line.ProductId);
                Console.WriteLine($"     • Остаток {line.ProductId}: {stock?.Quantity} шт.");
            }

            Console.WriteLine("\n  -> Проверка обработки ошибок (несуществующий файл):");
            try
            {
                await invoiceImportService.ImportAsync("nonexistent.txt");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"     ✓ Ошибка перехвачена: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  -> Ошибка импорта: {ex.Message}");
        }
    }


}