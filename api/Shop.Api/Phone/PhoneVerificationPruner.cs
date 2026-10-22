using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;

namespace Shop.Api.Phone;

/// <summary>
/// Deletes verification rows once they are a day old: by then the code has long expired and the
/// row has left the daily send window, so nothing reads it again. Runs at startup and then hourly.
/// The free host sleeps after 15 idle minutes, so the startup run is the one that usually happens.
/// </summary>
public sealed partial class PhoneVerificationPruner(
    IServiceScopeFactory scopes,
    ILogger<PhoneVerificationPruner> logger) : BackgroundService
{
    private const int BatchSize = 500;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    /// <summary>
    /// Loads and removes in batches rather than issuing one bulk DELETE, so the same code runs
    /// against the in-memory test database. Returns how many rows went.
    /// </summary>
    public static async Task<int> PruneAsync(ShopDbContext db, DateTime now, CancellationToken ct)
    {
        var cutoff = now.AddDays(-1);
        var total = 0;

        while (true)
        {
            var stale = await db.PhoneVerifications
                .Where(v => v.CreatedAt < cutoff)
                .OrderBy(v => v.Id)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (stale.Count == 0)
            {
                return total;
            }

            db.PhoneVerifications.RemoveRange(stale);
            await db.SaveChangesAsync(ct);
            db.ChangeTracker.Clear();
            total += stale.Count;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            await PruneOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PruneOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
            var removed = await PruneAsync(db, DateTime.UtcNow, ct);
            LogPruned(removed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Shutting down.
        }
        // A failed prune is retried on the next tick. Letting it escape would stop the whole API,
        // which is the hosting default for a background service that throws.
        catch (Exception exception)
        {
            LogPruneFailed(exception);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pruned {Count} old phone verification rows.")]
    private partial void LogPruned(int count);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Pruning old phone verification rows failed; will retry.")]
    private partial void LogPruneFailed(Exception exception);
}
