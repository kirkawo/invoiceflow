using System.Collections.Concurrent;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Infrastructure.Repositories;

public class InMemoryReminderRepository : IReminderRepository
{
    private readonly ConcurrentDictionary<Guid, Reminder> _store = new();

    public Guid? FilterWorkspaceId { get; set; }

    public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        _store[reminder.Id] = reminder;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Reminder>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var query = _store.Values.Where(r => r.InvoiceId == invoiceId);
        if (FilterWorkspaceId.HasValue)
            query = query.Where(r => r.WorkspaceId == FilterWorkspaceId.Value);
        return Task.FromResult<IReadOnlyList<Reminder>>(query.ToList().AsReadOnly());
    }
}
