using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class InvoiceTests
{
    private static readonly Guid ClientId = Guid.NewGuid();
    private static readonly DateTime IssueDate = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid WorkspaceId = Guid.NewGuid();

    private Invoice CreateInvoice(DateTime? dueDate = null) =>
        new(WorkspaceId, ClientId, "INV-001", IssueDate, dueDate ?? IssueDate.AddDays(30), "USD");

    private static InvoiceLineItem Line(string desc, decimal qty, decimal price) =>
        new(desc, qty, price);

    private Invoice CreateIssuedInvoice()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Service", 1, 500));
        invoice.Issue();
        return invoice;
    }

    [Fact]
    public void Total_EqualsSumOfLineItemAmounts()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Consulting", 10, 100));
        invoice.AddLineItem(Line("Hosting", 1, 50));

        Assert.Equal(10 * 100 + 1 * 50, invoice.Total);
    }

    [Fact]
    public void Subtotal_EqualsTotal_WhenNoTaxOrDiscount()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Design", 5, 200));

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
        invoice.AddLineItem(Line("Work", 1, 300));
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
        invoice.AddLineItem(Line("Service", 1, 500));

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

        Assert.Throws<InvalidOperationException>(() => invoice.AddLineItem(Line("Extra", 1, 100)));
    }

    [Fact]
    public void RemoveLineItem_Throws_WhenNotDraft()
    {
        var invoice = CreateInvoice();
        var item = Line("Extra", 1, 100);
        invoice.AddLineItem(item);
        invoice.Issue();

        Assert.Throws<InvalidOperationException>(() => invoice.RemoveLineItem(item));
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
        invoice.AddLineItem(Line("Item", 1, 100));

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
