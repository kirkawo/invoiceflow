using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

public class EfInvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceFlowDbContext _context;

    public EfInvoiceRepository(InvoiceFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Include(i => i.LineItems)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _context.Invoices.Update(invoice);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Invoices
            .Where(i => i.ClientId == clientId)
            .Include(i => i.LineItems)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
