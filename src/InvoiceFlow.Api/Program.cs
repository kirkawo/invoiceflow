using System.Security.Claims;
using InvoiceFlow.Application;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Api.Endpoints;
using InvoiceFlow.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<InvoiceFlowDbContext>()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/login";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
        await context.Database.MigrateAsync();

        if (!await context.Workspaces.AnyAsync())
        {
            var workspace = new Workspace("Default");
            context.Workspaces.Add(workspace);
            await context.SaveChangesAsync();

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser
            {
                UserName = "admin@invoiceflow.dev",
                Email = "admin@invoiceflow.dev",
                EmailConfirmed = true,
                WorkspaceId = workspace.Id
            };
            var result = await userManager.CreateAsync(user, "Admin123!");
        }
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (LoginRequest request, SignInManager<ApplicationUser> signInManager) =>
{
    var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, true, false);
    return result.Succeeded ? Results.Ok() : Results.Unauthorized();
});

app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Ok();
});

app.MapClientEndpoints();
app.MapInvoiceEndpoints();
app.MapClientInvoiceEndpoints();

app.Run();

public partial class Program { }

public record LoginRequest(string Email, string Password);

public class ApplicationUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public ApplicationUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        Microsoft.Extensions.Options.IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("WorkspaceId", user.WorkspaceId.ToString()));
        return identity;
    }
}
