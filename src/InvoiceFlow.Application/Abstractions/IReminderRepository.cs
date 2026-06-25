using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Abstractions;

public interface IReminderRepository
{
    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reminder>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default);
}
