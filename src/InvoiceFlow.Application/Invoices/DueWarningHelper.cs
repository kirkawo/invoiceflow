using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Invoices;

public record DueWarningInfo(string Text, string CssClass);

public static class DueWarningHelper
{
    public static DueWarningInfo? GetDueWarning(InvoiceStatus status, DateTime dueDateUtc)
    {
        var today = DateTime.UtcNow.Date;
        var dueDate = dueDateUtc.Date;
        var daysUntilDue = (dueDate - today).Days;

        if (status == InvoiceStatus.Issued)
        {
            if (daysUntilDue == 0)
                return new DueWarningInfo("Due today", "text-warning fw-semibold");

            if (daysUntilDue is >= 1 and <= 3)
            {
                var dayLabel = daysUntilDue == 1 ? "day" : "days";
                return new DueWarningInfo($"Due in {daysUntilDue} {dayLabel}", "text-info fw-semibold");
            }
        }

        if (status == InvoiceStatus.Overdue && daysUntilDue < 0)
        {
            var daysOverdue = -daysUntilDue;
            var dayLabel = daysOverdue == 1 ? "day" : "days";
            return new DueWarningInfo($"Overdue by {daysOverdue} {dayLabel}", "text-danger fw-semibold");
        }

        return null;
    }
}
