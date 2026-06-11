using System.Collections.Concurrent;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Infrastructure.Repositories;

public class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly ConcurrentDictionary<Guid, Invoice> _store = new();

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Invoice>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values.Where(i => i.ClientId == clientId).ToList().AsReadOnly());
}
