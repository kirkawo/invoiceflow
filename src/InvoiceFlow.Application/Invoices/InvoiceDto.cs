using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public class InvoiceDto
{
    public Guid Id { get; init; }
    public Guid ClientId { get; init; }
    public string Number { get; init; } = string.Empty;
    public DateTime IssueDateUtc { get; init; }
    public DateTime DueDateUtc { get; init; }
    public InvoiceStatus Status { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal Total { get; init; }
}
