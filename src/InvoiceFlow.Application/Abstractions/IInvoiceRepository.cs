using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invoice>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default);
}
