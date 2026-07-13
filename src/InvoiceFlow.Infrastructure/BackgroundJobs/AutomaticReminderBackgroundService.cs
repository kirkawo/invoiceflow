using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.BackgroundJobs;

public class AutomaticReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AutomaticReminderBackgroundService> _logger;
    private readonly ReminderOptions _options;

    public AutomaticReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AutomaticReminderBackgroundService> logger,
        ReminderOptions options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Automatic reminder background service started. Interval: {IntervalHours}h.",
            _options.CheckIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllWorkspacesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in automatic reminder cycle.");
            }

            await Task.Delay(TimeSpan.FromHours(_options.CheckIntervalHours), stoppingToken);
        }
    }

    private async Task ProcessAllWorkspacesAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        _logger.LogDebug("Starting automatic reminder cycle at {UtcNow}.", utcNow);

        List<Guid> workspaceIds;
        using (var scope = _scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
            workspaceIds = await dbContext.Workspaces
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);
        }

        var totalSynced = 0;
        var totalSent = 0;
        var processedCount = 0;

        foreach (var workspaceId in workspaceIds)
        {
            processedCount++;
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var syncService = scope.ServiceProvider.GetRequiredService<InvoiceStatusSyncService>();
                var syncCount = await syncService.SyncOverdueStatusAsync(workspaceId, utcNow, cancellationToken);
                if (syncCount > 0)
                {
                    _logger.LogInformation(
                        "Workspace {WorkspaceIndex}/{WorkspaceTotal} (Id={WorkspaceId}): {Count} invoice(s) transitioned to Overdue.",
                        processedCount, workspaceIds.Count, workspaceId, syncCount);
                    totalSynced += syncCount;
                }

                var reminderService = scope.ServiceProvider.GetRequiredService<AutomaticReminderService>();
                var sentCount = await reminderService.SendAutoRemindersAsync(workspaceId, utcNow, cancellationToken);
                totalSent += sentCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing workspace {WorkspaceIndex}/{WorkspaceTotal} (Id={WorkspaceId}).",
                    processedCount, workspaceIds.Count, workspaceId);
            }
        }

        _logger.LogInformation(
            "Reminder cycle complete. Workspaces checked: {WorkspaceCount}, overdue synced: {TotalSynced}, reminders sent: {TotalSent}.",
            workspaceIds.Count, totalSynced, totalSent);
    }
}
