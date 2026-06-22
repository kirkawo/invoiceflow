using System.Collections.Concurrent;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Infrastructure.Repositories;

public class InMemoryClientRepository : IClientRepository
{
    private readonly ConcurrentDictionary<Guid, Client> _store = new();

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var client) ? client : null);

    public Task<Client?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var client) ? client : null);

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _store[client.Id] = client;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Client>>(_store.Values.ToList().AsReadOnly());
}
