using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.Options;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class OverdueIntegrationTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();
    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;
    private readonly FakeReminderRepository _reminderRepository;
    private readonly FakeEmailSender _emailSender;
    private readonly FakeCurrentWorkspaceService _workspaceService;
    private readonly InvoiceStatusSyncService _syncService;
    private readonly AutomaticReminderService _reminderService;

    public OverdueIntegrationTests()
    {
        _invoiceRepository = new FakeInvoiceRepository { FilterWorkspaceId = TestWorkspaceId };
        _clientRepository = new FakeClientRepository();
        _reminderRepository = new FakeReminderRepository { FilterWorkspaceId = TestWorkspaceId };
        _emailSender = new FakeEmailSender();
        _workspaceService = new FakeCurrentWorkspaceService { WorkspaceId = TestWorkspaceId };
        _syncService = new InvoiceStatusSyncService(_invoiceRepository);
        _reminderService = new AutomaticReminderService(
            _invoiceRepository,
            _reminderRepository,
            _clientRepository,
            _emailSender,
            _workspaceService,
            new ReminderOptions
            {
                OverdueThresholdDays = 1,
                CooldownDays = 7,
                MaxAutoReminders = 2
            });
    }

    private async Task<(Guid clientId, Guid invoiceId)> CreateIssuedInvoiceAsync(
        DateTime dueDateUtc,
        string email = "client@example.com")
    {
        var client = new Client(TestWorkspaceId, "Test Client", email);
        await _clientRepository.AddAsync(client);

        var issueDate = dueDateUtc.AddDays(-30);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, dueDateUtc, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);
        return (client.Id, invoice.Id);
    }

    [Fact]
    public async Task SyncOverdue_ThenSendReminders_IssuedInvoiceBecomesOverdueAndGetsReminder()
    {
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var (_, invoiceId) = await CreateIssuedInvoiceAsync(dueDate);

        var syncCount = await _syncService.SyncOverdueStatusAsync(now);
        Assert.Equal(1, syncCount);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);

        var reminderCount = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(1, reminderCount);

        var reminders = _reminderRepository.All
            .Where(r => r.InvoiceId == invoiceId && r.Type == ReminderType.AutomaticOverdue)
            .ToList();
        Assert.Single(reminders);
        Assert.Equal(ReminderStatus.Sent, reminders[0].Status);
    }

    [Fact]
    public async Task PaidInvoice_DoesNotBecomeOverdue_DoesNotGetReminder()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", dueDate.AddDays(-30), dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.MarkPaid();
        await _invoiceRepository.AddAsync(invoice);

        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        var syncCount = await _syncService.SyncOverdueStatusAsync(now);
        Assert.Equal(0, syncCount);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);

        var reminderCount = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(0, reminderCount);
        Assert.Empty(_reminderRepository.All);
    }

    [Fact]
    public async Task CancelledInvoice_DoesNotBecomeOverdue_DoesNotGetReminder()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", dueDate.AddDays(-30), dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.Cancel();
        await _invoiceRepository.AddAsync(invoice);

        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);

        var syncCount = await _syncService.SyncOverdueStatusAsync(now);
        Assert.Equal(0, syncCount);
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);

        var reminderCount = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(0, reminderCount);
        Assert.Empty(_reminderRepository.All);
    }

    [Fact]
    public async Task RepeatedRuns_DoNotSpamReminders()
    {
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var (_, invoiceId) = await CreateIssuedInvoiceAsync(dueDate);

        await _syncService.SyncOverdueStatusAsync(now);
        var count1 = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(1, count1);

        var count2 = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(0, count2);

        var count3 = await _reminderService.SendAutoRemindersAsync(now);
        Assert.Equal(0, count3);

        var reminders = _reminderRepository.All
            .Where(r => r.InvoiceId == invoiceId && r.Type == ReminderType.AutomaticOverdue)
            .ToList();
        Assert.Single(reminders);
    }

    [Fact]
    public async Task SyncOverdue_IssuedInvoiceDueToday_DoesNotBecomeOverdue()
    {
        var dueDate = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        var (_, invoiceId) = await CreateIssuedInvoiceAsync(dueDate);

        var now = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);
        var syncCount = await _syncService.SyncOverdueStatusAsync(now);
        Assert.Equal(0, syncCount);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public async Task FullLifecycle_IssueToOverdueToReminderToPaid()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", dueDate.AddDays(-30), dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        var run1 = new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc);
        await _syncService.SyncOverdueStatusAsync(run1);
        var r1 = await _reminderService.SendAutoRemindersAsync(run1);
        Assert.Equal(0, r1);

        var run2 = new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc);
        var sync2 = await _syncService.SyncOverdueStatusAsync(run2);
        Assert.Equal(1, sync2);
        var r2 = await _reminderService.SendAutoRemindersAsync(run2);
        Assert.Equal(1, r2);

        var fetched = await _invoiceRepository.GetByIdAsync(invoice.Id);
        Assert.NotNull(fetched);
        fetched.MarkPaid();
        await _invoiceRepository.UpdateAsync(fetched);

        var run3 = new DateTime(2026, 7, 14, 12, 0, 0, DateTimeKind.Utc);
        var sync3 = await _syncService.SyncOverdueStatusAsync(run3);
        Assert.Equal(0, sync3);
        var r3 = await _reminderService.SendAutoRemindersAsync(run3);
        Assert.Equal(0, r3);

        var finalInvoice = await _invoiceRepository.GetByIdAsync(invoice.Id);
        Assert.NotNull(finalInvoice);
        Assert.Equal(InvoiceStatus.Paid, finalInvoice.Status);

        Assert.Single(_reminderRepository.All.Where(r => r.Type == ReminderType.AutomaticOverdue));
    }
}
