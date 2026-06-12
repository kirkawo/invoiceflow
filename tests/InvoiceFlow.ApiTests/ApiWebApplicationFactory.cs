using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.ApiTests;

public class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var clientRepo = services.SingleOrDefault(d => d.ServiceType == typeof(IClientRepository));
            if (clientRepo is not null) services.Remove(clientRepo);

            var invoiceRepo = services.SingleOrDefault(d => d.ServiceType == typeof(IInvoiceRepository));
            if (invoiceRepo is not null) services.Remove(invoiceRepo);

            var workspaceService = services.SingleOrDefault(d => d.ServiceType == typeof(ICurrentWorkspaceService));
            if (workspaceService is not null) services.Remove(workspaceService);

            services.AddSingleton<IClientRepository, InMemoryClientRepository>();
            services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
            services.AddSingleton<ICurrentWorkspaceService>(new FakeCurrentWorkspaceService());

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultScheme = TestAuthHandler.SchemeName;
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });

            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null);
        });
    }
}

public class FakeCurrentWorkspaceService : ICurrentWorkspaceService
{
    public Guid WorkspaceId => new("11111111-1111-1111-1111-111111111111");
    public void SetWorkspaceId(Guid id) { }
}
