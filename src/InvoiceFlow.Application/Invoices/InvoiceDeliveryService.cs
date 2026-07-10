using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Application.Invoices;

public class InvoiceDeliveryService
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IReminderRepository _reminderRepository;
    private readonly ICurrentWorkspaceService _workspaceService;
    private readonly IEmailSender _emailSender;
    private readonly AppOptions _appOptions;

    public InvoiceDeliveryService(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IReminderRepository reminderRepository,
        ICurrentWorkspaceService workspaceService,
        IEmailSender emailSender,
        IOptions<AppOptions> appOptions)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _reminderRepository = reminderRepository;
        _workspaceService = workspaceService;
        _emailSender = emailSender;
        _appOptions = appOptions.Value;
    }

    public async Task<ReminderDto> SendInvoiceEmailAsync(
        Guid invoiceId,
        string? customMessage = null,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId, cancellationToken)
            ?? throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        if (invoice.WorkspaceId != _workspaceService.WorkspaceId)
            throw new InvalidOperationException($"Invoice with ID '{invoiceId}' not found.");

        if (invoice.Status is InvoiceStatus.Draft or InvoiceStatus.Cancelled)
            throw new InvalidOperationException($"Cannot send an invoice in '{invoice.Status}' status.");

        if (invoice.LineItems.Count == 0)
            throw new InvalidOperationException("Cannot send an invoice without line items.");

        var client = await _clientRepository.GetByIdAsync(invoice.ClientId, cancellationToken)
            ?? throw new InvalidOperationException($"Client with ID '{invoice.ClientId}' not found.");

        if (string.IsNullOrWhiteSpace(client.Email))
            throw new InvalidOperationException("Cannot send invoice: client has no email address.");

        var publicBase = !string.IsNullOrWhiteSpace(_appOptions.PublicBaseUrl)
            ? _appOptions.PublicBaseUrl.TrimEnd('/')
            : _appOptions.BaseUrl.TrimEnd('/');

        var publicUrl = $"{publicBase}/invoices/public/{invoice.PublicId}";

        var subject = $"Invoice {invoice.Number} from InvoiceFlow";
        var body = BuildEmailBody(invoice, client, publicUrl, customMessage);

        var success = await _emailSender.TrySendAsync(client.Email, subject, body, cancellationToken);

        var reminder = new Reminder(
            _workspaceService.WorkspaceId,
            invoiceId,
            ReminderType.InvoiceSent,
            ReminderChannel.Email,
            client.Email,
            subject,
            success ? ReminderStatus.Sent : ReminderStatus.Failed,
            success ? null : "Email delivery failed.");

        await _reminderRepository.AddAsync(reminder, cancellationToken);

        return MapToDto(reminder);
    }

    public async Task<IReadOnlyList<ReminderDto>> GetDeliveryHistoryAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default)
    {
        var allReminders = await _reminderRepository.ListByInvoiceAsync(invoiceId, cancellationToken);
        return allReminders
            .Where(r => r.WorkspaceId == _workspaceService.WorkspaceId && r.Type == ReminderType.InvoiceSent)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();
    }

    private static string BuildEmailBody(Invoice invoice, Client client, string publicUrl, string? customMessage)
    {
        var lines = new List<string>
        {
            $"Dear {client.Name},",
            "",
            $"Your invoice {invoice.Number} is available at the secure link below.",
            "",
            $"Amount: {invoice.Total:F2} {invoice.Currency}",
            $"Issue date: {invoice.IssueDateUtc:yyyy-MM-dd}",
            $"Due date: {invoice.DueDateUtc:yyyy-MM-dd}",
            "",
            $"View your invoice:",
            publicUrl,
        };

        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            lines.Add("");
            lines.Add(customMessage);
        }

        lines.Add("");
        lines.Add("Thank you for your business.");
        lines.Add("InvoiceFlow");

        return string.Join("\n", lines);
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
