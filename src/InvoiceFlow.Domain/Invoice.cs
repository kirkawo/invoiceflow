namespace InvoiceFlow.Domain;

public class Invoice
{
    private static int _nextTempId = -1;
    private readonly List<InvoiceLineItem> _lineItems = [];

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid ClientId { get; private set; }
    public string Number { get; private set; }
    public DateTime IssueDateUtc { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public string Currency { get; private set; }
    public string? Notes { get; set; }
    public DateTime? IssuedAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();
    public decimal Subtotal => _lineItems.Sum(li => li.Amount);
    public decimal Total => Subtotal;

    private Invoice() => (Number, Currency) = (null!, null!);

    public Invoice(
        Guid workspaceId,
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
        WorkspaceId = workspaceId;
        ClientId = clientId;
        Number = number;
        IssueDateUtc = issueDateUtc;
        DueDateUtc = dueDateUtc;
        Status = InvoiceStatus.Draft;
        Currency = currency;
        Notes = notes;
    }

    public void AddLineItem(string description, decimal quantity, decimal unitPrice)
    {
        EnsureDraft();
        var tempId = Interlocked.Decrement(ref _nextTempId);
        var item = new InvoiceLineItem(tempId, description, quantity, unitPrice);
        _lineItems.Add(item);
    }

    public void UpdateLineItem(int lineItemId, string description, decimal quantity, decimal unitPrice)
    {
        EnsureDraft();
        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId)
            ?? throw new InvalidOperationException($"Line item with ID '{lineItemId}' not found.");
        item.Update(description, quantity, unitPrice);
    }

    public bool RemoveLineItem(int lineItemId)
    {
        EnsureDraft();
        var item = _lineItems.FirstOrDefault(li => li.Id == lineItemId);
        return item is not null && _lineItems.Remove(item);
    }

    public void UpdateDraft(
        Guid clientId,
        string number,
        DateTime issueDateUtc,
        DateTime dueDateUtc,
        string currency)
    {
        EnsureDraft();
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        if (dueDateUtc < issueDateUtc)
            throw new ArgumentException("Due date cannot be earlier than issue date.", nameof(dueDateUtc));

        ClientId = clientId;
        Number = number;
        IssueDateUtc = issueDateUtc;
        DueDateUtc = dueDateUtc;
        Currency = currency;
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be issued.");

        if (_lineItems.Count == 0)
            throw new InvalidOperationException("Cannot issue an invoice without at least one line item.");

        Status = InvoiceStatus.Issued;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public void MarkPaid()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid.");
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Cannot mark a cancelled invoice as paid.");
        if (Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot mark a draft invoice as paid. Issue it first.");

        Status = InvoiceStatus.Paid;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void MarkOverdue()
    {
        if (Status != InvoiceStatus.Issued)
            throw new InvalidOperationException("Only issued invoices can be marked overdue.");

        Status = InvoiceStatus.Overdue;
    }

    public void Cancel()
    {
        if (Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Cannot cancel a paid invoice.");
        if (Status == InvoiceStatus.Cancelled)
            throw new InvalidOperationException("Invoice is already cancelled.");

        Status = InvoiceStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
    }

    private void EnsureDraft()
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot modify a {Status.ToString().ToLower()} invoice.");
    }
}
