using WarehouseLib.Models;

namespace WarehouseLib.Services;

public interface IInvoiceImporter
{
    Task<Invoice> ImportAsync(string filePath);
}