using System.Security.Claims;
using InvoiceFlow.Application;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Infrastructure;
using InvoiceFlow.Infrastructure.BackgroundJobs;
using InvoiceFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using InvoiceFlow.Mvc.Models;
using InvoiceFlow.Pdf;
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
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.ReturnUrlParameter = "returnUrl";
});

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "InvoiceFlow.Web.Antiforgery";
    options.Cookie.HttpOnly = true;
});

builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<AutomaticReminderBackgroundService>();
builder.Services.AddHostedService<InvoiceStatusSyncBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

if (app.Configuration.GetValue<bool>("HttpsRedirect"))
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    await using var scope = app.Services.CreateAsyncScope();
    var context = scope.ServiceProvider.GetRequiredService<InvoiceFlowDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed on attempt {Attempt}.", attempt);
            break;
        }
        catch when (attempt < maxAttempts)
        {
            logger.LogWarning(
                "Database migration attempt {Attempt}/{MaxAttempts} failed. Retrying in 3s...",
                attempt, maxAttempts);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "publicInvoice",
    pattern: "invoices/public/{publicId}",
    defaults: new { controller = "Public", action = "Invoice" });

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

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
