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
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("WorkspaceId")
                ?? throw new UnauthorizedAccessException("User is not authenticated.");
            return Guid.Parse(claim.Value);
        }
    }
}
