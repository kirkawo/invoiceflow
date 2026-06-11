using InvoiceFlow.Api.Endpoints.Contracts;
using InvoiceFlow.Application.Invoices;

namespace InvoiceFlow.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices");

        group.MapPost("/", async (CreateInvoiceDraftRequest request, InvoiceService invoiceService) =>
        {
            try
            {
                var id = await invoiceService.CreateInvoiceDraftAsync(
                    request.ClientId,
                    request.Number,
                    request.IssueDateUtc,
                    request.DueDateUtc,
                    request.Currency,
                    request.Notes);

                return Results.Created($"/api/invoices/{id}", new { id });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        group.MapGet("/{id:guid}", async (Guid id, InvoiceService invoiceService) =>
        {
            var invoice = await invoiceService.GetInvoiceAsync(id);
            return invoice is not null ? Results.Ok(invoice) : Results.NotFound();
        });

        return app;
    }

    public static IEndpointRouteBuilder MapClientInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/clients")
            .MapGet("/{clientId:guid}/invoices", async (Guid clientId, InvoiceService invoiceService) =>
            {
                var invoices = await invoiceService.GetClientInvoicesAsync(clientId);
                return Results.Ok(invoices);
            });

        return app;
    }
}
