namespace InvoiceFlow.Application.Invoices;

public class InvoiceLineItemDto
{
    public int Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
}
