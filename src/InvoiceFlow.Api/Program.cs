using System.Security.Claims;
using System.Text.Json.Serialization;
using InvoiceFlow.Application;
using Microsoft.AspNetCore.DataProtection;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Pdf;
using InvoiceFlow.Api.Endpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.local.json",
        optional: true,
        reloadOnChange: true);
}

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

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

if (app.Configuration.GetValue<bool>("HttpsRedirect"))
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapClientEndpoints();
app.MapInvoiceEndpoints();
app.MapClientInvoiceEndpoints();
app.MapPublicEndpoints();

app.Run();

public partial class Program { }

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
