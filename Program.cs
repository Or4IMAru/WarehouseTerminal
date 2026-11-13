using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Variant10.Models;

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

        // TODO 2: Реализовать приход и расход товара.

        // TODO 3: Обработать несколько операций одновременно без потери обновлений.
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
}