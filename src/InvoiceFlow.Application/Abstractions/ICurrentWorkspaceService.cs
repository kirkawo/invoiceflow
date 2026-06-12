namespace InvoiceFlow.Application.Abstractions;

public interface ICurrentWorkspaceService
{
    Guid WorkspaceId { get; }
}
