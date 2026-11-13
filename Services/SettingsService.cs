using Microsoft.Win32;
using System.Text.Json;
using WarehouseLib.Models;

namespace Variant10.Services;

public sealed class SettingsService
{
    private const string RegistryPath = @"Software\Variant10\Warehouse";
    private const string FallbackFile = "warehouse_settings.json";

    public Task SaveAsync(WarehouseSettings settings)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));

        if (IsWindows())
        {
            SaveToRegistry(settings);
            Console.WriteLine("  -> Настройки сохранены в реестр HKCU.");
        }
        else
        {
            SaveToFile(settings);
            Console.WriteLine("  -> Настройки сохранены в JSON-файл (реестр недоступен на этой платформе).");
        }

        return Task.CompletedTask;
    }

    public Task<WarehouseSettings> LoadAsync()
    {
        WarehouseSettings settings;

        if (IsWindows())
        {
            settings = LoadFromRegistry();
            Console.WriteLine("  -> Настройки загружены из реестра HKCU.");
        }
        else
        {
            settings = LoadFromFile();
            Console.WriteLine("  -> Настройки загружены из JSON-файла (реестр недоступен).");
        }

        return Task.FromResult(settings);
    }


    private static void SaveToRegistry(WarehouseSettings settings)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath);
        key.SetValue(nameof(settings.WarehouseName), settings.WarehouseName);
        key.SetValue(nameof(settings.WarehousePath), settings.WarehousePath);
        key.SetValue(nameof(settings.DefaultCurrency), settings.DefaultCurrency);
        key.SetValue(nameof(settings.MinStockWarning), settings.MinStockWarning);
    }

    private static WarehouseSettings LoadFromRegistry()
    {
        var settings = new WarehouseSettings();
        using var key = Registry.CurrentUser.OpenSubKey(RegistryPath);
        if (key == null) return settings;

        if (key.GetValue(nameof(settings.WarehouseName)) is string name)
            settings.WarehouseName = name;
        if (key.GetValue(nameof(settings.WarehousePath)) is string path)
            settings.WarehousePath = path;
        if (key.GetValue(nameof(settings.DefaultCurrency)) is string currency)
            settings.DefaultCurrency = currency;
        if (key.GetValue(nameof(settings.MinStockWarning)) is int minStock)
            settings.MinStockWarning = minStock;

        return settings;
    }


    private static void SaveToFile(WarehouseSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FallbackFile, json);
    }

    private static WarehouseSettings LoadFromFile()
    {
        if (!File.Exists(FallbackFile))
            return new WarehouseSettings();

        var json = File.ReadAllText(FallbackFile);
        return JsonSerializer.Deserialize<WarehouseSettings>(json) ?? new WarehouseSettings();
    }


    private static bool IsWindows() =>
        OperatingSystem.IsWindows();
}