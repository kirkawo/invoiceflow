using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Application.Invoices;

public class InvoiceStatusSyncService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<InvoiceStatusSyncService> _logger;

    public InvoiceStatusSyncService(IInvoiceRepository invoiceRepository, ILogger<InvoiceStatusSyncService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    public async Task<int> SyncOverdueStatusAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var candidates = await _invoiceRepository.GetOverdueCandidatesAsync(utcNow, cancellationToken);
        return await TransitionOverdueInvoicesAsync(candidates, utcNow, cancellationToken);
    }

    public async Task<int> SyncOverdueStatusAsync(Guid workspaceId, DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var candidates = await _invoiceRepository.GetOverdueCandidatesAsync(workspaceId, utcNow, cancellationToken);
        return await TransitionOverdueInvoicesAsync(candidates, utcNow, cancellationToken);
    }

    private async Task<int> TransitionOverdueInvoicesAsync(
        IReadOnlyList<Invoice> candidates,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var invoice in candidates)
        {
            invoice.MarkOverdue();
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            count++;
        }

        if (count > 0)
        {
            _logger.LogInformation(
                "Overdue sync: {Count} invoice(s) transitioned to Overdue status.", count);
        }

        return count;
    }
}
