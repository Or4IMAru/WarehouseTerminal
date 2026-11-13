namespace WarehouseLib.Models;

public sealed class WarehouseSettings
{
    public string WarehouseName { get; set; } = "Главный склад";
    public string WarehousePath { get; set; } = @"C:\Warehouse";
    public string DefaultCurrency { get; set; } = "RUB";
    public int MinStockWarning { get; set; } = 10;

    public override string ToString() =>
        $"Название: {WarehouseName}, Путь: {WarehousePath}, " +
        $"Валюта: {DefaultCurrency}, Мин. остаток для предупреждения: {MinStockWarning}";
}