using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class AutomaticReminderServiceTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();
    private readonly AutomaticReminderService _service;
    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;
    private readonly FakeReminderRepository _reminderRepository;
    private readonly FakeEmailSender _emailSender;

    public AutomaticReminderServiceTests()
    {
        _invoiceRepository = new FakeInvoiceRepository { FilterWorkspaceId = TestWorkspaceId };
        _clientRepository = new FakeClientRepository();
        _reminderRepository = new FakeReminderRepository { FilterWorkspaceId = TestWorkspaceId };
        _emailSender = new FakeEmailSender();
        _service = new AutomaticReminderService(
            _invoiceRepository,
            _reminderRepository,
            _clientRepository,
            _emailSender);
    }

    private async Task<Guid> CreateClientAsync(string email = "client@example.com")
    {
        var client = new Client(TestWorkspaceId, "Test Client", email);
        await _clientRepository.AddAsync(client);
        return client.Id;
    }

    private async Task<Guid> CreateOverdueInvoiceAsync(Guid clientId, DateTime dueDateUtc)
    {
        var issueDate = dueDateUtc.AddDays(-30);
        var invoice = new Invoice(TestWorkspaceId, clientId, "INV-001", issueDate, dueDateUtc, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.MarkOverdue();
        await _invoiceRepository.AddAsync(invoice);
        return invoice.Id;
    }

    private async Task AddAutoReminderAsync(Guid invoiceId, DateTime sentAtUtc)
    {
        var reminder = new Reminder(
            TestWorkspaceId,
            invoiceId,
            ReminderType.AutomaticOverdue,
            ReminderChannel.Email,
            "client@example.com",
            "Reminder for INV-001",
            ReminderStatus.Sent);
        var field = typeof(Reminder).GetField("<SentAtUtc>k__BackingField",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(reminder, sentAtUtc);
        await _reminderRepository.AddAsync(reminder);
    }

    // --- First auto reminder ---

    [Fact]
    public async Task OverdueInvoice_NoAutoReminders_DueAtLeastOneDay_SendsFirstAutoReminder()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc); // 5 days overdue
        var invoiceId = await CreateOverdueInvoiceAsync(clientId, dueDate);

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(1, count);
        var autoReminders = _reminderRepository.All
            .Where(r => r.Type == ReminderType.AutomaticOverdue)
            .ToList();
        Assert.Single(autoReminders);
    }

    [Fact]
    public async Task OverdueInvoice_NoAutoReminders_SameDayDueDate_NoReminder()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc); // 0 days overdue
        await CreateOverdueInvoiceAsync(clientId, dueDate);

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(0, count);
    }

    // --- Second auto reminder ---

    [Fact]
    public async Task OverdueInvoice_OneAutoReminder_SevenOrMoreDaysAgo_SendsSecond()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await CreateOverdueInvoiceAsync(clientId, dueDate);
        await AddAutoReminderAsync(invoiceId, new DateTime(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc)); // 7 days ago

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(1, count);
        Assert.Equal(2, _reminderRepository.All.Count(r => r.Type == ReminderType.AutomaticOverdue));
    }

    [Fact]
    public async Task OverdueInvoice_OneAutoReminder_LessThanSevenDays_NoSecond()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await CreateOverdueInvoiceAsync(clientId, dueDate);
        await AddAutoReminderAsync(invoiceId, new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc)); // 5 days ago

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(0, count);
    }

    // --- Two auto reminders already sent ---

    [Fact]
    public async Task OverdueInvoice_TwoAutoReminders_NoMoreSent()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await CreateOverdueInvoiceAsync(clientId, dueDate);
        await AddAutoReminderAsync(invoiceId, new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc));
        await AddAutoReminderAsync(invoiceId, new DateTime(2026, 7, 20, 12, 0, 0, DateTimeKind.Utc));

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(0, count);
    }

    // --- No client email ---

    [Fact]
    public async Task OverdueInvoice_NoClientEmail_NoReminder()
    {
        var clientId = await CreateClientAsync("client@example.com");
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        await CreateOverdueInvoiceAsync(clientId, dueDate);

        // Wipe client email via wrapped repo
        var clientRepoNoEmail = new ClientNoEmailRepo(_clientRepository, clientId);
        var service = new AutomaticReminderService(
            _invoiceRepository, _reminderRepository, clientRepoNoEmail, _emailSender);

        var count = await service.SendAutoRemindersAsync(now);

        Assert.Equal(0, count);
    }

    // --- Non-overdue invoice ---

    [Fact]
    public async Task NonOverdueInvoice_NoReminder()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var issueDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, clientId, "INV-002", issueDate, issueDate.AddDays(30), "USD");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice); // Status = Issued, not Overdue

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(0, count);
    }

    // --- Idempotency ---

    [Fact]
    public async Task MultipleRuns_Idempotent_NoDuplicateAutoReminders()
    {
        var clientId = await CreateClientAsync();
        var now = DateTime.UtcNow;
        var dueDate = now.Date.AddDays(-5);
        await CreateOverdueInvoiceAsync(clientId, dueDate);

        var run1 = await _service.SendAutoRemindersAsync(now);
        var run2 = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(1, run1);
        Assert.Equal(0, run2);
        Assert.Single(_reminderRepository.All.Where(r => r.Type == ReminderType.AutomaticOverdue));
    }

    // --- Manual reminders don't count toward limit ---

    [Fact]
    public async Task OverdueInvoice_WithManualRemindersOnly_SendsFirstAutoReminder()
    {
        var clientId = await CreateClientAsync();
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        await CreateOverdueInvoiceAsync(clientId, dueDate);

        var invoiceId = (await _invoiceRepository.ListAllAsync()).First().Id;
        var manualReminder = new Reminder(
            TestWorkspaceId, invoiceId, ReminderType.ManualOverdue, ReminderChannel.Email,
            "client@example.com", "Manual", ReminderStatus.Sent);
        await _reminderRepository.AddAsync(manualReminder);

        var count = await _service.SendAutoRemindersAsync(now);

        Assert.Equal(1, count);
        Assert.Equal(2, _reminderRepository.All.Count); // manual + auto
    }

    // --- Workspace isolation ---

    [Fact]
    public async Task OverdueInvoice_DifferentWorkspace_CreatesNoReminderInTestWorkspace()
    {
        var otherWsId = Guid.NewGuid();
        var clientId = await CreateClientAsync("other@example.com");
        var now = new DateTime(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(otherWsId, clientId, "INV-OTHER", dueDate.AddDays(-30), dueDate, "USD");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        invoice.MarkOverdue();
        await _invoiceRepository.AddAsync(invoice);

        var count = await _service.SendAutoRemindersAsync(now);

        var ourReminders = _reminderRepository.All
            .Where(r => r.WorkspaceId == TestWorkspaceId)
            .ToList();
        Assert.Empty(ourReminders);
    }
}
