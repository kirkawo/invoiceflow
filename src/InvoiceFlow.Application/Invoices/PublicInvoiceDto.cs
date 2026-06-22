using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public class PublicInvoiceDto
{
    public string Number { get; init; } = string.Empty;
    public string ClientName { get; init; } = string.Empty;
    public string? ClientEmail { get; init; }
    public string? ClientCompany { get; init; }
    public InvoiceStatus Status { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public DateTime IssueDateUtc { get; init; }
    public DateTime DueDateUtc { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<InvoiceLineItemDto> LineItems { get; init; } = [];
}
