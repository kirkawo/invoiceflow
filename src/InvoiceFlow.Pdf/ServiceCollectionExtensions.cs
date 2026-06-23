using InvoiceFlow.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Pdf;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdf(this IServiceCollection services)
    {
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        return services;
    }
}
