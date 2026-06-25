using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvoiceFlow.Infrastructure.Persistence.Repositories;

public class EfReminderRepository : IReminderRepository
{
    private readonly InvoiceFlowDbContext _context;
    private readonly ICurrentWorkspaceService _workspaceService;

    public EfReminderRepository(InvoiceFlowDbContext context, ICurrentWorkspaceService workspaceService)
    {
        _context = context;
        _workspaceService = workspaceService;
    }

    public async Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        await _context.Set<Reminder>().AddAsync(reminder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var workspaceId = _workspaceService.WorkspaceId;
        return await _context.Set<Reminder>()
            .Where(r => r.WorkspaceId == workspaceId && r.InvoiceId == invoiceId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
