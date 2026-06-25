using InvoiceFlow.Application.Clients;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Reminders;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ClientService>();
        services.AddScoped<InvoiceService>();
        services.AddScoped<PublicInvoiceService>();
        services.AddScoped<InvoiceStatusSyncService>();
        services.AddScoped<ManualReminderService>();
        return services;
    }
}
