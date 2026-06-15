using System.Security.Claims;
using InvoiceFlow.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Infrastructure.Persistence;

public class CurrentWorkspaceService : ICurrentWorkspaceService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentWorkspaceService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid WorkspaceId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst("WorkspaceId");
            if (claim is null || !Guid.TryParse(claim.Value, out var workspaceId))
            {
                throw new UnauthorizedAccessException("Workspace has not been initialized.");
            }
            return workspaceId;
        }
    }
}
