namespace InvoiceFlow.Domain;

public class Invoice
{
    private readonly List<InvoiceLineItem> _lineItems = [];

    public Guid Id { get; }
    public Guid ClientId { get; }
    public string Number { get; }
    public DateTime IssueDateUtc { get; }
    public DateTime DueDateUtc { get; }
    public InvoiceStatus Status { get; private set; }
    public string Currency { get; }
    public string? Notes { get; }
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal Subtotal => _lineItems.Sum(li => li.Amount);
    public decimal Total => Subtotal;

    public Invoice(
        Guid clientId,
        string number,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        string currency,
        string? notes = null)
    {
        if (dueDateUtc < issueDateUtc)
            throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(dueDateUtc));

        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        Id = Guid.NewGuid();
        ClientId = clientId;
        Number = number;
        IssueDateUtc = issueDateUtc;
        DueDateUtc = dueDateUtc;
        Status = InvoiceStatus.Draft;
        Currency = currency;
        Notes = notes;
    }

    public void AddLineItem(InvoiceLineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _lineItems.Add(item);
    }

    public bool RemoveLineItem(InvoiceLineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return _lineItems.Remove(item);
    }

    public void Issue()
    {
        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Cannot issue an invoice without at least one line item.");

        Status = InvoiceStatus.Issued;
    }

    public void MarkAsPaid()
    {
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot mark a cancelled invoice as paid.");

        Status = InvoiceStatus.Paid;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");

        Status = InvoiceStatus.Cancelled;
    }
}
