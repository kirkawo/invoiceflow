namespace InvoiceFlow.Domain;

public class InvoiceLineItem
{
    public int Id { get; private set; }
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
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

    internal InvoiceLineItem(int id, string description, decimal quantity, decimal unitPrice)
        : this(description, quantity, unitPrice)
    {
        Id = id;
    }

    internal void Update(string description, decimal quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
