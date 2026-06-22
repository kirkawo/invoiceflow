using System.Security.Claims;
using InvoiceFlow.Application;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Pdf;
using InvoiceFlow.Web.Components;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPdf();

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
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapPost("/auth/login", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].FirstOrDefault();
    var password = form["password"].FirstOrDefault();
    var returnUrl = form["returnUrl"].FirstOrDefault() ?? "/";

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
