using InvoiceFlow.Api.Endpoints.Contracts;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices").RequireAuthorization();

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

        group.MapGet("/", async (Guid? clientId, InvoiceStatus? status, InvoiceService invoiceService) =>
        {
            var invoices = await invoiceService.GetAllInvoicesAsync(clientId, status);
            return Results.Ok(invoices);
        });

        group.MapGet("/{id:guid}", async (Guid id, InvoiceService invoiceService) =>
        {
            var invoice = await invoiceService.GetInvoiceAsync(id);
            return invoice is not null ? Results.Ok(invoice) : Results.NotFound();
        });

        group.MapPost("/{id:guid}/issue", async (Guid id, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.IssueInvoiceAsync(id);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:guid}/mark-paid", async (Guid id, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.MarkInvoicePaidAsync(id);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:guid}/mark-overdue", async (Guid id, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.MarkInvoiceOverdueAsync(id);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:guid}/cancel", async (Guid id, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.CancelInvoiceAsync(id);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPost("/{id:guid}/line-items", async (Guid id, AddLineItemRequest request, InvoiceService invoiceService) =>
        {
            try
            {
                var lineItemId = await invoiceService.AddLineItemAsync(id, request.Description, request.Quantity, request.UnitPrice);
                return Results.Created($"/api/invoices/{id}/line-items/{lineItemId}", new { lineItemId });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapPut("/{id:guid}/line-items/{lineItemId:int}", async (Guid id, int lineItemId, UpdateLineItemRequest request, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.UpdateLineItemAsync(id, lineItemId, request.Description, request.Quantity, request.UnitPrice);
                return Results.Ok();
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapDelete("/{id:guid}/line-items/{lineItemId:int}", async (Guid id, int lineItemId, InvoiceService invoiceService) =>
        {
            try
            {
                await invoiceService.RemoveLineItemAsync(id, lineItemId);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/{id:guid}/pdf", async (Guid id, InvoiceService invoiceService, IInvoicePdfService pdfService) =>
        {
            var invoiceDto = await invoiceService.GetInvoiceAsync(id);
            if (invoiceDto is null)
                return Results.NotFound();

            var invoice = await invoiceService.LoadInvoiceDomainAsync(id);
            if (invoice is null)
                return Results.NotFound();

            if (invoice.LineItems.Count == 0)
                return Results.BadRequest(new { error = "Cannot generate PDF for an invoice without line items." });

            var pdf = pdfService.GeneratePdf(invoice);
            return Results.File(pdf, "application/pdf", $"invoice-{invoiceDto.Number}.pdf");
        });

        group.MapPost("/{id:guid}/reminders/manual", async (Guid id, ManualReminderService reminderService) =>
        {
            try
            {
                var reminder = await reminderService.SendManualReminderAsync(id);
                return Results.Created($"/api/invoices/{id}/reminders", reminder);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        group.MapGet("/{id:guid}/reminders", async (Guid id, ManualReminderService reminderService) =>
        {
            var reminders = await reminderService.GetReminderHistoryAsync(id);
            return Results.Ok(reminders);
        });

        return app;
    }

    public static IEndpointRouteBuilder MapClientInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/clients").RequireAuthorization()
            .MapGet("/{clientId:guid}/invoices", async (Guid clientId, InvoiceService invoiceService) =>
            {
                var invoices = await invoiceService.GetClientInvoicesAsync(clientId);
                return Results.Ok(invoices);
            });

        return app;
    }
}
