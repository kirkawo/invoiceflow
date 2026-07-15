using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class DueWarningHelperTests
{
    private static readonly DateOnly Today = new(2026, 6, 24);

    [Fact]
    public void Issued_DueToday_ReturnsDueToday()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Due today", result.Text);
        Assert.Equal("text-warning fw-semibold", result.CssClass);
    }

    [Fact]
    public void Issued_DueInOneDay_ReturnsDueIn1Day()
    {
        var dueDate = Today.AddDays(1);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Due in 1 day", result.Text);
        Assert.Equal("text-info fw-semibold", result.CssClass);
    }

    [Fact]
    public void Issued_DueInThreeDays_ReturnsDueIn3Days()
    {
        var dueDate = Today.AddDays(3);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Due in 3 days", result.Text);
    }

    [Fact]
    public void Issued_DueInFourDays_ReturnsNull()
    {
        var dueDate = Today.AddDays(4);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.Null(result);
    }

    [Fact]
    public void Issued_DueInPast_ReturnsOverdueWarning()
    {
        var dueDate = Today.AddDays(-1);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Overdue by 1 day", result.Text);
        Assert.Equal("text-danger fw-semibold", result.CssClass);
    }

    [Fact]
    public void Overdue_DueFiveDaysAgo_ReturnsOverdueBy5Days()
    {
        var dueDate = Today.AddDays(-5);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Overdue by 5 days", result.Text);
        Assert.Equal("text-danger fw-semibold", result.CssClass);
    }

    [Fact]
    public void Overdue_DueOneDayAgo_ReturnsOverdueBy1Day()
    {
        var dueDate = Today.AddDays(-1);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.NotNull(result);
        Assert.Equal("Overdue by 1 day", result.Text);
    }

    [Fact]
    public void Overdue_DueToday_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.Null(result);
    }

    [Fact]
    public void Draft_ReturnsNull()
    {
        var dueDate = Today.AddDays(2);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Draft, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.Null(result);
    }

    [Fact]
    public void Paid_ReturnsNull()
    {
        var dueDate = Today.AddDays(2);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Paid, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.Null(result);
    }

    [Fact]
    public void Cancelled_ReturnsNull()
    {
        var dueDate = Today.AddDays(2);
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Cancelled, dueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), Today);

        Assert.Null(result);
    }
}
