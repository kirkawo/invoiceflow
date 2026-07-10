using System.Security.Claims;
using InvoiceFlow.Application;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Domain;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Pdf;
using InvoiceFlow.Web.Components;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

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

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "DataProtection-Keys")))
    .SetApplicationName("InvoiceFlow");

builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));

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
    options.Cookie.Name = "InvoiceFlow.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.LoginPath = "/login";
    options.LogoutPath = "/auth/logout";
    options.AccessDeniedPath = "/login";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "InvoiceFlow.Web.Antiforgery";
    options.Cookie.HttpOnly = true;
});

var app = builder.Build();

if (app.Environment.IsProduction())
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
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

    if (config.GetValue<bool>("SampleData:Enabled"))
    {
        var refresh = config.GetValue<bool>("SampleData:RefreshOnStartup");
        var append = !refresh && config.GetValue<bool>("SampleData:AppendOnStartup");
        await InvoiceFlowSampleDataSeeder.SeedAsync(context, refresh, append);
    }
}
else
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("HttpsRedirect"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/register", async (HttpContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, InvoiceFlowDbContext db) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].FirstOrDefault();
    var password = form["password"].FirstOrDefault();
    var confirmPassword = form["confirmPassword"].FirstOrDefault();
    var returnUrl = form["returnUrl"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";
    }

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString("Email and password are required.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    if (password != confirmPassword)
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString("Passwords do not match.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    if (password.Length < 6)
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString("Password must be at least 6 characters.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var existingUser = await userManager.FindByEmailAsync(email);
    if (existingUser is not null)
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString("An account with this email already exists.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var workspace = new Workspace($"{email}'s Workspace");
    db.Workspaces.Add(workspace);
    await db.SaveChangesAsync();

    var user = new ApplicationUser
    {
        UserName = email,
        Email = email,
        WorkspaceId = workspace.Id,
    };

    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync();

        var errors = string.Join(" ", result.Errors.Select(e => e.Description));
        return Results.Redirect($"/register?error={Uri.EscapeDataString(errors)}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    await signInManager.SignInAsync(user, true);
    return Results.Redirect(returnUrl);
});

app.MapPost("/auth/login", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].FirstOrDefault();
    var password = form["password"].FirstOrDefault();
    var returnUrl = form["returnUrl"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(returnUrl) || !Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
    {
        returnUrl = "/";
    }

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect($"/login?error={Uri.EscapeDataString("Email and password are required.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    var result = await signInManager.PasswordSignInAsync(email, password, true, false);

    if (result.Succeeded)
    {
        return Results.Redirect(returnUrl);
    }

    return Results.Redirect($"/login?error={Uri.EscapeDataString("Invalid email or password.")}&returnUrl={Uri.EscapeDataString(returnUrl)}");
});

app.MapPost("/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/login");
}).RequireAuthorization();

app.MapGet("/invoices/{id:guid}/pdf", async (Guid id, InvoiceService invoiceService, IInvoicePdfService pdfService) =>
{
    var invoice = await invoiceService.LoadInvoiceDomainAsync(id);
    if (invoice is null)
        return Results.NotFound();

    if (invoice.LineItems.Count == 0)
        return Results.BadRequest(new { error = "Cannot generate PDF for an invoice without line items." });

    var pdf = pdfService.GeneratePdf(invoice);
    return Results.File(pdf, "application/pdf", $"invoice-{invoice.Number}.pdf");
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

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