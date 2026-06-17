using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class InvoiceTests
{
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly DateTime IssueDate = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private Invoice CreateInvoice(DateTime? dueDate = null) =>
        new(WorkspaceId, ClientId, "INV-001", IssueDate, dueDate ?? IssueDate.AddDays(30), "USD");

    private Invoice CreateIssuedInvoice()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Service", 1, 500);
        invoice.Issue();
        return invoice;
    }

    [Fact]
    public void Total_EqualsSumOfLineItemAmounts()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Consulting", 10, 100);
        invoice.AddLineItem("Hosting", 1, 50);

        Assert.Equal(10 * 100 + 1 * 50, invoice.Total);
    }

    [Fact]
    public void Subtotal_EqualsTotal_WhenNoTaxOrDiscount()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Design", 5, 200);

        Assert.Equal(invoice.Total, invoice.Subtotal);
    }

    [Fact]
    public void Issue_Throws_WhenNoLineItems()
    {
        var invoice = CreateInvoice();

        Assert.Throws<InvalidOperationException>(() => invoice.Issue());
    }

    [Fact]
    public void Issue_Throws_WhenAlreadyIssued()
    {
        var invoice = CreateIssuedInvoice();

        Assert.Throws<InvalidOperationException>(() => invoice.Issue());
    }

    [Fact]
    public void Issue_Throws_WhenAlreadyCancelled()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Work", 1, 300);
        invoice.Cancel();

        Assert.Throws<InvalidOperationException>(() => invoice.Issue());
    }

    [Fact]
    public void Issue_Throws_WhenAlreadyPaid()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.Issue());
    }

    [Fact]
    public void Issue_Succeeds_WhenLineItemsExist()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Service", 1, 500);

        invoice.Issue();

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
        Assert.NotNull(invoice.IssuedAtUtc);
    }

    [Fact]
    public void MarkPaid_Throws_WhenCancelled()
    {
        var invoice = CreateIssuedInvoice();
        invoice.Cancel();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkPaid());
    }

    [Fact]
    public void MarkPaid_Throws_WhenDraft()
    {
        var invoice = CreateInvoice();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkPaid());
    }

    [Fact]
    public void MarkPaid_Throws_WhenAlreadyPaid()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkPaid());
    }

    [Fact]
    public void MarkPaid_Succeeds_WhenIssued()
    {
        var invoice = CreateIssuedInvoice();

        invoice.MarkPaid();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.NotNull(invoice.PaidAtUtc);
    }

    [Fact]
    public void MarkPaid_Succeeds_WhenOverdue()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkOverdue();

        invoice.MarkPaid();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyPaid()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.Cancel());
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyCancelled()
    {
        var invoice = CreateInvoice();
        invoice.Cancel();

        Assert.Throws<InvalidOperationException>(() => invoice.Cancel());
    }

    [Fact]
    public void Cancel_Succeeds_WhenDraft()
    {
        var invoice = CreateInvoice();

        invoice.Cancel();

        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
        Assert.NotNull(invoice.CancelledAtUtc);
    }

    [Fact]
    public void Cancel_Succeeds_WhenIssued()
    {
        var invoice = CreateIssuedInvoice();

        invoice.Cancel();

        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
    }

    [Fact]
    public void Cancel_Succeeds_WhenOverdue()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkOverdue();

        invoice.Cancel();

        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
    }

    [Fact]
    public void MarkOverdue_Throws_WhenNotIssued()
    {
        var invoice = CreateInvoice();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkOverdue());
    }

    [Fact]
    public void MarkOverdue_Succeeds_WhenIssued()
    {
        var invoice = CreateIssuedInvoice();

        invoice.MarkOverdue();

        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public void MarkOverdue_Throws_WhenAlreadyPaid()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkOverdue());
    }

    [Fact]
    public void AddLineItem_Throws_WhenNotDraft()
    {
        var invoice = CreateIssuedInvoice();

        Assert.Throws<InvalidOperationException>(() => invoice.AddLineItem("Extra", 1, 100));
    }

    [Fact]
    public void RemoveLineItem_Throws_WhenNotDraft()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Extra", 1, 100);
        var itemId = invoice.LineItems[0].Id;
        invoice.Issue();

        Assert.Throws<InvalidOperationException>(() => invoice.RemoveLineItem(itemId));
    }

    [Fact]
    public void RemoveLineItem_RemovesFromList_WhenDraft()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item A", 1, 100);
        invoice.AddLineItem("Item B", 2, 50);
        var firstId = invoice.LineItems[0].Id;

        var removed = invoice.RemoveLineItem(firstId);

        Assert.True(removed);
        Assert.Single(invoice.LineItems);
        Assert.Equal("Item B", invoice.LineItems[0].Description);
    }

    [Fact]
    public void AddLineItem_UpdatesTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item", 2, 50);

        Assert.Equal(100, invoice.Total);
    }

    [Fact]
    public void RemoveLineItem_UpdatesTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item A", 2, 50);
        invoice.AddLineItem("Item B", 3, 30);
        var itemBId = invoice.LineItems[1].Id;

        invoice.RemoveLineItem(itemBId);

        Assert.Equal(100, invoice.Total);
    }

    [Fact]
    public void UpdateLineItem_UpdatesTotal()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item", 2, 50);
        var itemId = invoice.LineItems[0].Id;

        invoice.UpdateLineItem(itemId, "Updated Item", 3, 100);

        Assert.Equal(300, invoice.Total);
        Assert.Equal("Updated Item", invoice.LineItems[0].Description);
    }

    [Fact]
    public void UpdateLineItem_Throws_WhenNotDraft()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item", 1, 100);
        var itemId = invoice.LineItems[0].Id;
        invoice.Issue();

        Assert.Throws<InvalidOperationException>(() =>
            invoice.UpdateLineItem(itemId, "Changed", 2, 200));
    }

    [Fact]
    public void UpdateLineItem_Throws_WhenLineItemNotFound()
    {
        var invoice = CreateInvoice();

        Assert.Throws<InvalidOperationException>(() =>
            invoice.UpdateLineItem(999, "Ghost", 1, 100));
    }

    [Fact]
    public void RemoveLineItem_Throws_WhenLineItemNotFound()
    {
        var invoice = CreateInvoice();

        var removed = invoice.RemoveLineItem(999);

        Assert.False(removed);
    }

    [Fact]
    public void UpdateDraft_Throws_WhenNotDraft()
    {
        var invoice = CreateIssuedInvoice();

        Assert.Throws<InvalidOperationException>(() =>
            invoice.UpdateDraft(ClientId, "INV-002", IssueDate, IssueDate.AddDays(60), "EUR"));
    }

    [Fact]
    public void UpdateDraft_Succeeds_WhenDraft()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem("Item", 1, 100);

        invoice.UpdateDraft(ClientId, "INV-002", IssueDate, IssueDate.AddDays(60), "EUR");

        Assert.Equal("INV-002", invoice.Number);
        Assert.Equal("EUR", invoice.Currency);
        Assert.Equal(IssueDate.AddDays(60), invoice.DueDateUtc);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
        Assert.Single(invoice.LineItems);
    }

    [Fact]
    public void Paid_Invoice_IsFinalState()
    {
        var invoice = CreateIssuedInvoice();
        invoice.MarkPaid();

        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Constructor_Throws_WhenDueDateBeforeIssueDate()
    {
        var dueDate = IssueDate.AddDays(-1);

        Assert.Throws<ArgumentException>(() => CreateInvoice(dueDate));
    }

    [Fact]
    public void Constructor_Succeeds_WhenDueDateEqualsIssueDate()
    {
        var invoice = CreateInvoice(IssueDate);

        Assert.Equal(IssueDate, invoice.DueDateUtc);
    }
}
