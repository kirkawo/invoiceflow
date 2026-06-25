using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Reminders;

public class AutomaticReminderService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IReminderRepository _reminderRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IEmailSender _emailSender;
    private readonly ICurrentWorkspaceService _currentWorkspaceService;

    public AutomaticReminderService(
        IInvoiceRepository invoiceRepository,
        IReminderRepository reminderRepository,
        IClientRepository clientRepository,
        IEmailSender emailSender,
        ICurrentWorkspaceService currentWorkspaceService)
    {
        _invoiceRepository = invoiceRepository;
        _reminderRepository = reminderRepository;
        _clientRepository = clientRepository;
        _emailSender = emailSender;
        _currentWorkspaceService = currentWorkspaceService;
    }

    public async Task<int> SendAutoRemindersAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var allInvoices = await _invoiceRepository.ListAllAsync(cancellationToken);
        var overdueInvoices = allInvoices.Where(i => i.Status == InvoiceStatus.Overdue).ToList();
        var count = 0;

        foreach (var invoice in overdueInvoices)
        {
            if (!await TrySendAutoReminderAsync(invoice, utcNow, cancellationToken))
                continue;

            count++;
        }

        return count;
    }

    private async Task<bool> TrySendAutoReminderAsync(
        Invoice invoice,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (invoice.WorkspaceId != _currentWorkspaceService.WorkspaceId)
            return false;

        var client = await _clientRepository.GetByIdAsync(invoice.ClientId, cancellationToken);
        if (client is null || string.IsNullOrWhiteSpace(client.Email))
            return false;

        var reminders = await _reminderRepository.ListByInvoiceAsync(invoice.Id, cancellationToken);
        var autoReminders = reminders
            .Where(r => r.WorkspaceId == invoice.WorkspaceId && r.Type == ReminderType.AutomaticOverdue)
            .OrderBy(r => r.SentAtUtc)
            .ToList();

        if (autoReminders.Count == 0)
        {
            var daysOverdue = (utcNow.Date - invoice.DueDateUtc.Date).Days;
            if (daysOverdue < 1)
                return false;
        }
        else if (autoReminders.Count == 1)
        {
            var firstSent = autoReminders[0].SentAtUtc;
            if ((utcNow - firstSent).TotalDays < 7)
                return false;
        }
        else
        {
            return false;
        }

        var subject = $"Automatic Payment Reminder: Invoice {invoice.Number}";
        var body = $"Dear {client.Name},\n\nThis is an automatic reminder that Invoice {invoice.Number} for {invoice.Total:F2} {invoice.Currency} is overdue.\n\nDue date: {invoice.DueDateUtc:yyyy-MM-dd}\n\nPlease arrange payment at your earliest convenience.\n\nThank you.";

        var success = await _emailSender.TrySendAsync(client.Email, subject, body, cancellationToken);

        var reminder = new Reminder(
            invoice.WorkspaceId,
            invoice.Id,
            ReminderType.AutomaticOverdue,
            ReminderChannel.Email,
            client.Email,
            subject,
            success ? ReminderStatus.Sent : ReminderStatus.Failed,
            success ? null : "Email delivery failed.");

        await _reminderRepository.AddAsync(reminder, cancellationToken);

        return true;
    }
}
