using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IClientRepository, InMemoryClientRepository>();
        services.AddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();
        return services;
    }
}
