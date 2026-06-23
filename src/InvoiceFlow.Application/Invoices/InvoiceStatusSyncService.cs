using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public class InvoiceStatusSyncService
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceStatusSyncService(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public async Task<int> SyncOverdueStatusAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var candidates = await _invoiceRepository.GetOverdueCandidatesAsync(utcNow, cancellationToken);
        var count = 0;

        foreach (var invoice in candidates)
        {
            invoice.MarkOverdue();
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            count++;
        }

        return count;
    }
}
