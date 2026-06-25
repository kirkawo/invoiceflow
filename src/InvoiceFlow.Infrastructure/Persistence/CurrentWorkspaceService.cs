using System.Security.Claims;
using InvoiceFlow.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Infrastructure.Persistence;

public class CurrentWorkspaceService : ICurrentWorkspaceService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly AsyncLocal<Guid?> _backgroundWorkspace = new();

    public CurrentWorkspaceService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public static IDisposable PushWorkspace(Guid workspaceId)
    {
        var previous = _backgroundWorkspace.Value;
        _backgroundWorkspace.Value = workspaceId;
        return new WorkspaceRestorer(previous);
    }

    public Guid WorkspaceId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("WorkspaceId");
            if (claim is not null && Guid.TryParse(claim.Value, out var httpId))
                return httpId;

            if (_backgroundWorkspace.Value.HasValue)
                return _backgroundWorkspace.Value.Value;

            throw new UnauthorizedAccessException("Workspace has not been initialized.");
        }
    }

    private sealed class WorkspaceRestorer : IDisposable
    {
        private readonly Guid? _previous;
        public WorkspaceRestorer(Guid? previous) => _previous = previous;
        public void Dispose() => _backgroundWorkspace.Value = _previous;
    }
}
