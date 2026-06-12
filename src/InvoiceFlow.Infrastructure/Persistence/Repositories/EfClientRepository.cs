using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

public class EfClientRepository : IClientRepository
{
    private readonly InvoiceFlowDbContext _context;

    public EfClientRepository(InvoiceFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Clients.AsNoTracking().ToListAsync(cancellationToken);
    }
}
