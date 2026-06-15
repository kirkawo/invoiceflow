using Microsoft.AspNetCore.Identity;

namespace InvoiceFlow.Infrastructure.Persistence;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid WorkspaceId { get; set; }
}
