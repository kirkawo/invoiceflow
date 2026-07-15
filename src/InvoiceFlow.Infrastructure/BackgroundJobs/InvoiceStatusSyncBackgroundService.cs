using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.BackgroundJobs;

public class InvoiceStatusSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceStatusSyncBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public InvoiceStatusSyncBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceStatusSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice status sync background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllWorkspacesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in invoice status sync cycle.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task SyncAllWorkspacesAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        _logger.LogDebug("Starting overdue sync cycle at {UtcNow}.", utcNow);

        List<Guid> workspaceIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
            workspaceIds = await dbContext.Workspaces
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);
        }

        var totalSynced = 0;

        foreach (var workspaceId in workspaceIds)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<InvoiceStatusSyncService>();
                var count = await syncService.SyncOverdueStatusAsync(workspaceId, utcNow, cancellationToken);
                totalSynced += count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing overdue status for workspace {WorkspaceId}.", workspaceId);
            }
        }

        if (totalSynced > 0)
        {
            _logger.LogInformation(
                "Overdue sync complete. Workspaces checked: {WorkspaceCount}, invoices transitioned: {TotalSynced}.",
                workspaceIds.Count, totalSynced);
        }
    }
}
