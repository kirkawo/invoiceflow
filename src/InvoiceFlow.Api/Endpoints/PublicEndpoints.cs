using InvoiceFlow.Application.Invoices;

namespace InvoiceFlow.Api.Endpoints;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/public");

        group.MapGet("/invoices/{publicId}", async (string publicId, PublicInvoiceService publicInvoiceService) =>
        {
            var invoice = await publicInvoiceService.GetPublicInvoiceAsync(publicId);
            return invoice is not null ? Results.Ok(invoice) : Results.NotFound();
        });

        return app;
    }
}
