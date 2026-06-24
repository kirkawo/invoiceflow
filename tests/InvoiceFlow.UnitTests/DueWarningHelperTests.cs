using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class DueWarningHelperTests
{
    [Fact]
    public void Issued_DueToday_ReturnsDueToday()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, DateTime.UtcNow);

        Assert.NotNull(result);
        Assert.Equal("Due today", result.Text);
        Assert.Equal("text-warning fw-semibold", result.CssClass);
    }

    [Fact]
    public void Issued_DueInOneDay_ReturnsDueIn1Day()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, DateTime.UtcNow.AddDays(1));

        Assert.NotNull(result);
        Assert.Equal("Due in 1 day", result.Text);
        Assert.Equal("text-info fw-semibold", result.CssClass);
    }

    [Fact]
    public void Issued_DueInThreeDays_ReturnsDueIn3Days()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, DateTime.UtcNow.AddDays(3));

        Assert.NotNull(result);
        Assert.Equal("Due in 3 days", result.Text);
    }

    [Fact]
    public void Issued_DueInFourDays_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, DateTime.UtcNow.AddDays(4));

        Assert.Null(result);
    }

    [Fact]
    public void Issued_DueInPast_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Issued, DateTime.UtcNow.AddDays(-1));

        Assert.Null(result);
    }

    [Fact]
    public void Overdue_DueFiveDaysAgo_ReturnsOverdueBy5Days()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-5));

        Assert.NotNull(result);
        Assert.Equal("Overdue by 5 days", result.Text);
        Assert.Equal("text-danger fw-semibold", result.CssClass);
    }

    [Fact]
    public void Overdue_DueOneDayAgo_ReturnsOverdueBy1Day()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, DateTime.UtcNow.AddDays(-1));

        Assert.NotNull(result);
        Assert.Equal("Overdue by 1 day", result.Text);
    }

    [Fact]
    public void Overdue_DueToday_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Overdue, DateTime.UtcNow);

        Assert.Null(result);
    }

    [Fact]
    public void Draft_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Draft, DateTime.UtcNow.AddDays(2));

        Assert.Null(result);
    }

    [Fact]
    public void Paid_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Paid, DateTime.UtcNow.AddDays(2));

        Assert.Null(result);
    }

    [Fact]
    public void Cancelled_ReturnsNull()
    {
        var result = DueWarningHelper.GetDueWarning(InvoiceStatus.Cancelled, DateTime.UtcNow.AddDays(2));

        Assert.Null(result);
    }
}
