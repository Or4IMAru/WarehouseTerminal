using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Variant10.Models;
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

        // TODO 5: Запустить внешнюю программу-валидатор через Process.
        // TODO 6: Вынести складскую логику в DLL и подключить её через ProjectReference.
        // TODO 7: Сохранять настройки склада в HKCU.

        // TODO 8: Добавить адаптер для старого формата накладной.
        // TODO 9: Реализовать очередь операций через Channel.
        // TODO 10: Сформировать параллельный отчёт по категориям.

        Console.WriteLine("Стартовый проект собран. Реализуйте задания варианта.");
        await Task.CompletedTask;
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
}