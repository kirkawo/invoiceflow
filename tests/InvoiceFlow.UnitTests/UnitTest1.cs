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

    [Fact]
    public void Total_EqualsSumOfLineItemAmounts()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Consulting", 10, 100));
        invoice.AddLineItem(Line("Hosting", 1, 50));

        var total = invoice.Total;

        Assert.Equal(10 * 100 + 1 * 50, total);
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
    public void Issue_Succeeds_WhenLineItemsExist()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Service", 1, 500));

        invoice.Issue();

        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public void MarkAsPaid_Throws_WhenCancelled()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Work", 1, 300));
        invoice.Issue();
        invoice.Cancel();

        Assert.Throws<InvalidOperationException>(() => invoice.MarkAsPaid());
    }

    [Fact]
    public void Cancel_Throws_WhenAlreadyPaid()
    {
        var invoice = CreateInvoice();
        invoice.AddLineItem(Line("Work", 1, 300));
        invoice.Issue();
        invoice.MarkAsPaid();

        Assert.Throws<InvalidOperationException>(() => invoice.Cancel());
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
