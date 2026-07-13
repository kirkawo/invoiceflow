using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace InvoiceFlow.Web.Components;

public static class WorkspaceHelper
{
    public static async Task<Guid> ResolveWorkspaceIdAsync(AuthenticationStateProvider authStateProvider)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        var claim = user.FindFirst("WorkspaceId");
        if (claim is not null && Guid.TryParse(claim.Value, out var workspaceId))
            return workspaceId;

        throw new UnauthorizedAccessException("Unable to determine workspace from authentication state.");
    }
}
