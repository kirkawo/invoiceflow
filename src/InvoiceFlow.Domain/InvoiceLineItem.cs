namespace InvoiceFlow.Domain;

public class InvoiceLineItem
{
    public string Description { get; }
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal Amount => Quantity * UnitPrice;

    private InvoiceLineItem() => Description = null!;

    public InvoiceLineItem(string description, decimal quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
