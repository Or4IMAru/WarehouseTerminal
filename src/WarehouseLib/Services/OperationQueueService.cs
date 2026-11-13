using System.Threading.Channels;
using WarehouseLib.Models;

namespace WarehouseLib.Services;

public sealed class OperationQueueService : IAsyncDisposable
{
    private readonly WarehouseService _warehouse;
    private readonly Channel<StockOperation> _channel;
    private readonly Task _consumerTask;
    private readonly CancellationTokenSource _cts = new();

    private int _processedCount;
    private int _failedCount;

    public int ProcessedCount => _processedCount;
    public int FailedCount => _failedCount;
    public int PendingCount => _channel.Reader.Count;

    public OperationQueueService(WarehouseService warehouse, int capacity = 100)
    {
        _warehouse = warehouse ?? throw new ArgumentNullException(nameof(warehouse));

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        };
        _channel = Channel.CreateBounded<StockOperation>(options);

        _consumerTask = Task.Run(() => ConsumeAsync(_cts.Token));
    }

    public async Task EnqueueReceiveAsync(string productId, int quantity)
    {
        var op = new StockOperation
        {
            Type = StockOperationType.Receive,
            ProductId = productId,
            Quantity = quantity
        };

        await _channel.Writer.WriteAsync(op);

        await op.CompletionSource.Task;
    }

    public async Task<bool> EnqueueIssueAsync(string productId, int quantity)
    {
        var op = new StockOperation
        {
            Type = StockOperationType.Issue,
            ProductId = productId,
            Quantity = quantity
        };

        await _channel.Writer.WriteAsync(op);

        var result = await op.CompletionSource.Task;
        return result is true;
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var op in _channel.Reader.ReadAllAsync(ct))
            {
                try
                {
                    switch (op.Type)
                    {
                        case StockOperationType.Receive:
                            await _warehouse.ReceiveGoodsAsync(op.ProductId, op.Quantity);
                            op.CompletionSource.SetResult(null);
                            Interlocked.Increment(ref _processedCount);
                            break;

                        case StockOperationType.Issue:
                            var ok = await _warehouse.IssueGoodsAsync(op.ProductId, op.Quantity);
                            op.CompletionSource.SetResult(ok);
                            Interlocked.Increment(ref _processedCount);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    op.CompletionSource.SetException(ex);
                    Interlocked.Increment(ref _failedCount);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    public async Task StopAsync()
    {
        _channel.Writer.Complete();
        _cts.Cancel();
        try
        {
            await _consumerTask;
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }
}