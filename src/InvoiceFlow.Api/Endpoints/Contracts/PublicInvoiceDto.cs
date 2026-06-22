using InvoiceFlow.Application.Invoices;

namespace InvoiceFlow.Api.Endpoints.Contracts;

public record PublicInvoiceDto(
    string Number,
    string ClientName,
    string? ClientEmail,
    string? ClientCompany,
    string Status,
    string Currency,
    decimal Total,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    string? Notes,
    IReadOnlyList<InvoiceLineItemDto> LineItems);
