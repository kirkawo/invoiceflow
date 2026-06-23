using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

public class EfClientRepository : IClientRepository
{
    private readonly InvoiceFlowDbContext _context;
    private readonly ICurrentWorkspaceService _workspaceService;

    public EfClientRepository(InvoiceFlowDbContext context, ICurrentWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspaceId = _workspaceService.WorkspaceId;
        return await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default)
    {
        var workspaceId = _workspaceService.WorkspaceId;
        return await _context.Clients
            .Where(c => c.WorkspaceId == workspaceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
