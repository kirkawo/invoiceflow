using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddSingleton<IClientRepository, InMemoryClientRepository>();
            services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        });
    }
}
