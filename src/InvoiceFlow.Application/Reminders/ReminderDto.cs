using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Reminders;

public class ReminderDto
{
    public Guid Id { get; init; }
    public ReminderType Type { get; init; }
    public ReminderChannel Channel { get; init; }
    public string RecipientEmail { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public ReminderStatus Status { get; init; }
    public DateTime SentAtUtc { get; init; }
    public string? FailureReason { get; init; }
}
