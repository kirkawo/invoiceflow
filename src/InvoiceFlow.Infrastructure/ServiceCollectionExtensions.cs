using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Infrastructure.Persistence;
using InvoiceFlow.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default");

        services.AddDbContext<InvoiceFlowDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IClientRepository, EfClientRepository>();
        services.AddScoped<IInvoiceRepository, EfInvoiceRepository>();

        return services;
    }
}
