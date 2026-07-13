using System.Collections.Concurrent;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Infrastructure.Repositories;

public class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly ConcurrentDictionary<Guid, Invoice> _store = new();

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

    public Task<Invoice?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(i => i.PublicId == publicId));

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

    public Task<IReadOnlyList<Invoice>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values.ToList().AsReadOnly());

    public Task<IReadOnlyList<Invoice>> GetOverdueCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values
                .Where(i => i.Status == InvoiceStatus.Issued && i.DueDateUtc < utcNow)
                .ToList()
                .AsReadOnly());

    public Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var maxSeq = _store.Values
            .Where(i => i.Number.StartsWith(prefix))
            .Select(i => int.TryParse(i.Number[prefix.Length..], out var seq) ? seq : 0)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult($"{prefix}{(maxSeq + 1):D4}");
    }

    public Task<IReadOnlyList<Invoice>> ListAllAsync(Guid workspaceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values.Where(i => i.WorkspaceId == workspaceId).ToList().AsReadOnly());

    public Task<IReadOnlyList<Invoice>> GetOverdueCandidatesAsync(Guid workspaceId, DateTime utcNow, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values
                .Where(i => i.WorkspaceId == workspaceId && i.Status == InvoiceStatus.Issued && i.DueDateUtc < utcNow)
                .ToList()
                .AsReadOnly());
}
