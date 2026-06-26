using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.ApiTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Guid _workspaceId;

    public ApiWebApplicationFactory(Guid? workspaceId = null)
    {
        _workspaceId = workspaceId ?? new("11111111-1111-1111-1111-111111111111");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var clientRepo = services.SingleOrDefault(d => d.ServiceType == typeof(IClientRepository));
            if (clientRepo is not null) services.Remove(clientRepo);

            var invoiceRepo = services.SingleOrDefault(d => d.ServiceType == typeof(IInvoiceRepository));
            if (invoiceRepo is not null) services.Remove(invoiceRepo);

            var workspaceService = services.SingleOrDefault(d => d.ServiceType == typeof(ICurrentWorkspaceService));
            if (workspaceService is not null) services.Remove(workspaceService);

            var reminderRepo = services.SingleOrDefault(d => d.ServiceType == typeof(IReminderRepository));
            if (reminderRepo is not null) services.Remove(reminderRepo);

            var emailSender = services.SingleOrDefault(d => d.ServiceType == typeof(IEmailSender));
            if (emailSender is not null) services.Remove(emailSender);

            services.AddSingleton<IClientRepository, InMemoryClientRepository>();
            services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
            services.AddSingleton<IReminderRepository, InMemoryReminderRepository>();
            services.AddSingleton<IEmailSender>(new FakeEmailSender());
            services.AddSingleton<ICurrentWorkspaceService>(new FakeCurrentWorkspaceService(_workspaceId));

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);

            services.Configure<ReminderOptions>(o =>
            {
                o.CheckIntervalHours = 1;
                o.OverdueThresholdDays = 1;
                o.CooldownDays = 7;
                o.MaxAutoReminders = 2;
            });
            services.AddSingleton(sp => sp.GetRequiredService<IOptions<ReminderOptions>>().Value);
        });
    }
}

public class FakeCurrentWorkspaceService : ICurrentWorkspaceService
{
    public Guid WorkspaceId { get; }

    public FakeCurrentWorkspaceService(Guid workspaceId)
    {
        WorkspaceId = workspaceId;
    }
}

public class FakeEmailSender : IEmailSender
{
    public Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
