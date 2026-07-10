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
        _logger.LogInformation("Automatic reminder background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAllWorkspacesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing automatic reminders.");
            }

            await Task.Delay(TimeSpan.FromHours(_options.CheckIntervalHours), stoppingToken);
        }
    }

    private async Task ProcessAllWorkspacesAsync(CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        using (var syncScope = _scopeFactory.CreateScope())
        {
            var syncService = syncScope.ServiceProvider.GetRequiredService<InvoiceStatusSyncService>();
            var syncCount = await syncService.SyncOverdueStatusAsync(utcNow, cancellationToken);
            if (syncCount > 0)
            {
                _logger.LogInformation(
                    "Synced {Count} invoice(s) to Overdue status.", syncCount);
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
        var workspaceIds = await dbContext.Workspaces
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        foreach (var workspaceId in workspaceIds)
        {
            using var wsScope = _scopeFactory.CreateScope();
            using var _ = CurrentWorkspaceService.PushWorkspace(workspaceId);

            var service = wsScope.ServiceProvider.GetRequiredService<AutomaticReminderService>();
            var count = await service.SendAutoRemindersAsync(utcNow, cancellationToken);

            if (count > 0)
            {
                _logger.LogInformation(
                    "Sent {Count} automatic reminder(s) for workspace {WorkspaceId}.",
                    count, workspaceId);
            }
        }
    }
}
