using InvoiceFlow.Application.Clients;
using InvoiceFlow.Application.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientService>();
        services.AddScoped<InvoiceService>();
        return services;
    }
}
