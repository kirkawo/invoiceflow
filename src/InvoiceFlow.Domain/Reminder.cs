namespace InvoiceFlow.Domain;

public enum ReminderType { ManualOverdue, AutomaticOverdue, InvoiceSent }

public enum ReminderChannel { Email }

public enum ReminderStatus { Sent, Failed }

public class Reminder
{
    public Guid Id { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public ReminderType Type { get; private set; }
    public ReminderChannel Channel { get; private set; }
    public string RecipientEmail { get; private set; }
    public string Subject { get; private set; }
    public ReminderStatus Status { get; private set; }
    public DateTime SentAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private Reminder() => (RecipientEmail, Subject) = (null!, null!);

    public Reminder(
        Guid workspaceId,
        Guid invoiceId,
        ReminderType type,
        ReminderChannel channel,
        string recipientEmail,
        string subject,
        ReminderStatus status,
        string? failureReason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        InvoiceId = invoiceId;
        Type = type;
        Channel = channel;
        RecipientEmail = recipientEmail;
        Subject = subject;
        SentAtUtc = DateTime.UtcNow;
        CreatedAtUtc = DateTime.UtcNow;
        Status = status;
        FailureReason = failureReason;
    }
}
