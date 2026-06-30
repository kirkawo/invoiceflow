using System.Security.Claims;
using System.Text.Json.Serialization;
using InvoiceFlow.Application;
using Microsoft.AspNetCore.DataProtection;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Pdf;
using InvoiceFlow.Api.Endpoints;
using InvoiceFlow.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPdf();

var dataProtectionKeysPath = Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys");
Directory.CreateDirectory(dataProtectionKeysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("InvoiceFlow");

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

// builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
//     {
//         options.User.RequireUniqueEmail = true;
//         options.Password.RequireDigit = false;
//         options.Password.RequiredLength = 6;
//         options.Password.RequireNonAlphanumeric = false;
//         options.Password.RequireUppercase = false;
//     })
//     .AddEntityFrameworkStores<InvoiceFlowDbContext>()
//     .AddDefaultTokenProviders()
//     .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

// builder.Services.ConfigureApplicationCookie(options =>
// {
//     options.Cookie.Name = "InvoiceFlow.Api.Antiforgery";
//     options.Cookie.HttpOnly = true;
//     options.ExpireTimeSpan = TimeSpan.FromHours(24);
//     options.LoginPath = "/login";
//     options.ReturnUrlParameter = "returnUrl";
// });

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = false;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<InvoiceFlowDbContext>()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.Logger.LogInformation("BaseDirectory = {dir}", AppContext.BaseDirectory);

app.Logger.LogInformation("KeyPath = {path}", dataProtectionKeysPath);
app.Logger.LogInformation("Exists = {exists}", Directory.Exists(dataProtectionKeysPath));

if (Directory.Exists(dataProtectionKeysPath))
{
    foreach (var f in Directory.GetFiles(dataProtectionKeysPath))
    {
        app.Logger.LogInformation("Key file = {file}", f);
    }
}

// if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
// {
//     await using var scope = app.Services.CreateAsyncScope();
//     var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
//     await context.Database.MigrateAsync();
// }

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await context.Database.MigrateAsync();
            break;
        }
        catch when (attempt < maxAttempts)
        {
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await context.Workspaces.AnyAsync())
    {
        var workspace = new Workspace("Default");
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var user = new ApplicationUser
        {
            UserName = "admin@invoiceflow.dev",
            Email = "admin@invoiceflow.dev",
            EmailConfirmed = true,
            WorkspaceId = workspace.Id
        };
        await userManager.CreateAsync(user, "Admin123!");
    }
}

if (app.Environment.IsProduction() && app.Configuration.GetValue<bool>("Bootstrap:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    if (!await context.Workspaces.AnyAsync())
    {
        var workspace = new Workspace("Default");
        context.Workspaces.Add(workspace);
        await context.SaveChangesAsync();

        var email = app.Configuration.GetValue<string>("Bootstrap:AdminEmail") ?? "admin@invoiceflow.dev";
        var password = app.Configuration.GetValue<string>("Bootstrap:AdminPassword") ?? "Admin123!";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            WorkspaceId = workspace.Id
        };
        await userManager.CreateAsync(user, password);
    }
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

if (app.Configuration.GetValue<bool>("HttpsRedirect"))
{
    app.UseHttpsRedirection();
}

// app.UseAuthentication();
app.UseAuthorization();

// app.MapPost("/api/auth/login", async (LoginRequest request, SignInManager<ApplicationUser> signInManager) =>
// {
//     var result = await signInManager.PasswordSignInAsync(request.Email, request.Password, true, false);
//     return result.Succeeded ? Results.Ok() : Results.Unauthorized();
// });

// app.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
// {
//     await signInManager.SignOutAsync();
//     return Results.Ok();
// });

app.MapClientEndpoints();
app.MapInvoiceEndpoints();
app.MapClientInvoiceEndpoints();
app.MapPublicEndpoints();

app.Run();

public partial class Program { }

// public record LoginRequest(string Email, string Password);

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
