using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Application.Reminders;

public class ManualReminderService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentWorkspaceService _workspaceService;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ManualReminderService> _logger;

    public ManualReminderService(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IReminderRepository reminderRepository,
        ICurrentWorkspaceService workspaceService,
        IEmailSender emailSender,
        ILogger<ManualReminderService> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _reminderRepository = reminderRepository;
        _workspaceService = workspaceService;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<ReminderDto> SendManualReminderAsync(
        Guid invoiceId,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        if (invoice.WorkspaceId != _workspaceService.WorkspaceId)
            throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        if (invoice.Status != InvoiceStatus.Overdue)
            throw new InvalidOperationException("Manual reminders can only be sent for overdue invoices.");

        var client = await _clientRepository.GetByIdAsync(invoice.ClientId, cancellationToken)
            ?? throw new InvalidOperationException($"Client with ID '{invoice.ClientId}' not found.");

        if (string.IsNullOrWhiteSpace(client.Email))
            throw new InvalidOperationException("Cannot send reminder: client has no email address.");

        var subject = $"Payment Reminder: Invoice {invoice.Number}";
        var body = $"Dear {client.Name},\n\nThis is a reminder that Invoice {invoice.Number} for {invoice.Total:F2} {invoice.Currency} is overdue.\n\nDue date: {invoice.DueDateUtc:yyyy-MM-dd}\n\nPlease arrange payment at your earliest convenience.\n\nThank you.";

        if (!string.IsNullOrWhiteSpace(message))
            body += $"\n\n{message}";

        var success = await _emailSender.TrySendAsync(client.Email, subject, body, cancellationToken);

        var reminder = new Reminder(
            _workspaceService.WorkspaceId,
            invoiceId,
            ReminderType.ManualOverdue,
            ReminderChannel.Email,
            client.Email,
            subject,
            success ? ReminderStatus.Sent : ReminderStatus.Failed,
            success ? null : "Email delivery failed.");

        await _reminderRepository.AddAsync(reminder, cancellationToken);

        if (success)
        {
            _logger.LogInformation(
                "Manual reminder sent for Invoice {InvoiceNumber} (Id={InvoiceId}) to {Email}.",
                invoice.Number, invoiceId, client.Email);
        }
        else
        {
            _logger.LogWarning(
                "Manual reminder failed for Invoice {InvoiceNumber} (Id={InvoiceId}) to {Email}.",
                invoice.Number, invoiceId, client.Email);
        }

        return MapToDto(reminder);
    }

    public async Task<IReadOnlyList<ReminderDto>> GetReminderHistoryAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var reminders = await _reminderRepository.ListByInvoiceAsync(invoiceId, cancellationToken);
        return reminders
            .Where(r => r.WorkspaceId == _workspaceService.WorkspaceId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();
    }

    private static ReminderDto MapToDto(Reminder reminder) => new()
    {
        Id = reminder.Id,
        Type = reminder.Type,
        Channel = reminder.Channel,
        RecipientEmail = reminder.RecipientEmail,
        Subject = reminder.Subject,
        Status = reminder.Status,
        SentAtUtc = reminder.SentAtUtc,
        CreatedAtUtc = reminder.CreatedAtUtc,
        FailureReason = reminder.FailureReason
    };
}
