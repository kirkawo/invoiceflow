using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Abstractions;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid id, Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Client>> ListAsync(Guid workspaceId, CancellationToken cancellationToken = default);
}
