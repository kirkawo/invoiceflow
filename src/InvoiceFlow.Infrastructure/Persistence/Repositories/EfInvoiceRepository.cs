using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

public class EfInvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceFlowDbContext _context;
    private readonly ICurrentWorkspaceService _workspaceService;

    public EfInvoiceRepository(InvoiceFlowDbContext context, ICurrentWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = _workspaceService.WorkspaceId;
        return await _context.Invoices
            .Include(i => i.LineItems)
            .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Id == id, cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Invoices.AddAsync(invoice, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var workspaceId = _workspaceService.WorkspaceId;
        return await _context.Invoices
            .Where(i => i.WorkspaceId == workspaceId && i.ClientId == clientId)
            .Include(i => i.LineItems)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
