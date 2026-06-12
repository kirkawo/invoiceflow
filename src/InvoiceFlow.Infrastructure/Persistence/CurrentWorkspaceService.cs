using InvoiceFlow.Application.Abstractions;

namespace InvoiceFlow.Infrastructure.Persistence;

public class CurrentWorkspaceService : ICurrentWorkspaceService
{
    private Guid? _workspaceId;

    public Guid WorkspaceId => _workspaceId ?? throw new UnauthorizedAccessException("Workspace has not been initialized.");

    public void SetWorkspaceId(Guid id) => _workspaceId = id;
}
