using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Reminders;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class ManualReminderServiceTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();
    private readonly ManualReminderService _service;
    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;
    private readonly FakeReminderRepository _reminderRepository;
    private readonly FakeEmailSender _emailSender;

    public ManualReminderServiceTests()
    {
        _invoiceRepository = new FakeInvoiceRepository { FilterWorkspaceId = TestWorkspaceId };
        _clientRepository = new FakeClientRepository();
        _reminderRepository = new FakeReminderRepository { FilterWorkspaceId = TestWorkspaceId };
        _emailSender = new FakeEmailSender();
        _service = new ManualReminderService(
            _invoiceRepository,
            _clientRepository,
            _reminderRepository,
            new FakeCurrentWorkspaceService { WorkspaceId = TestWorkspaceId },
            _emailSender);
    }

    private async Task<(Guid invoiceId, Guid clientId)> CreateOverdueInvoiceAsync(string clientEmail = "client@example.com")
    {
        var client = new Client(TestWorkspaceId, "Test Client", clientEmail);
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.MarkOverdue();
        await _invoiceRepository.AddAsync(invoice);

        return (invoice.Id, client.Id);
    }

    [Fact]
    public async Task SendManualReminderAsync_OverdueInvoiceWithEmail_CreatesReminder()
    {
        var (invoiceId, _) = await CreateOverdueInvoiceAsync();

        var result = await _service.SendManualReminderAsync(invoiceId);

        Assert.NotNull(result);
        Assert.Equal(ReminderStatus.Sent, result.Status);
        Assert.Equal("client@example.com", result.RecipientEmail);
        Assert.Equal(ReminderType.ManualOverdue, result.Type);
        Assert.Equal(ReminderChannel.Email, result.Channel);
        Assert.Contains("INV-001", result.Subject);
    }

    [Fact]
    public async Task SendManualReminderAsync_OverdueInvoiceWithEmail_StoresReminderInRepository()
    {
        var (invoiceId, _) = await CreateOverdueInvoiceAsync();

        await _service.SendManualReminderAsync(invoiceId);

        var reminders = await _reminderRepository.ListByInvoiceAsync(invoiceId);
        Assert.Single(reminders);
        Assert.Equal(ReminderStatus.Sent, reminders[0].Status);
    }

    [Fact]
    public async Task SendManualReminderAsync_NonOverdueInvoice_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, issueDate.AddDays(30), "USD");
        await _invoiceRepository.AddAsync(invoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendManualReminderAsync(invoice.Id));
    }

    [Fact]
    public async Task SendManualReminderAsync_MissingClientEmail_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "dummy@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.MarkOverdue();
        await _invoiceRepository.AddAsync(invoice);

        var wrappedClientRepo = new ClientNoEmailRepo(_clientRepository, client.Id);
        var service = new ManualReminderService(
            _invoiceRepository, wrappedClientRepo, _reminderRepository,
            new FakeCurrentWorkspaceService { WorkspaceId = TestWorkspaceId }, _emailSender);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendManualReminderAsync(invoice.Id));
    }

    [Fact]
    public async Task SendManualReminderAsync_EmptyClientEmail_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "dummy@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, dueDate, "USD");
        invoice.AddLineItem("Service", 1, 100);
        invoice.Issue();
        invoice.MarkOverdue();
        await _invoiceRepository.AddAsync(invoice);

        var wrappedClientRepo = new ClientEmptyEmailRepo(_clientRepository, client.Id);
        var service = new ManualReminderService(
            _invoiceRepository, wrappedClientRepo, _reminderRepository,
            new FakeCurrentWorkspaceService { WorkspaceId = TestWorkspaceId }, _emailSender);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendManualReminderAsync(invoice.Id));
    }

    [Fact]
    public async Task SendManualReminderAsync_InvoiceInDifferentWorkspace_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var otherWsId = Guid.NewGuid();
        var issueDate = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var otherInvoice = new Invoice(otherWsId, client.Id, "INV-OTHER", issueDate, issueDate.AddDays(14), "USD");
        otherInvoice.AddLineItem("Item", 1, 100);
        otherInvoice.Issue();
        otherInvoice.MarkOverdue();
        await _invoiceRepository.AddAsync(otherInvoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendManualReminderAsync(otherInvoice.Id));
    }

    [Fact]
    public async Task GetReminderHistoryAsync_ReturnsOnlyRemindersForInvoice()
    {
        var (invoiceId1, _) = await CreateOverdueInvoiceAsync("c1@example.com");
        var (invoiceId2, _) = await CreateOverdueInvoiceAsync("c2@example.com");

        await _service.SendManualReminderAsync(invoiceId1);
        await _service.SendManualReminderAsync(invoiceId2);
        await _service.SendManualReminderAsync(invoiceId2);

        var history = await _service.GetReminderHistoryAsync(invoiceId1);

        Assert.Single(history);
        Assert.Equal("c1@example.com", history[0].RecipientEmail);
    }

    [Fact]
    public async Task GetReminderHistoryAsync_ReturnsNewestFirst()
    {
        var (invoiceId, _) = await CreateOverdueInvoiceAsync();

        var r1 = await _service.SendManualReminderAsync(invoiceId);
        var r2 = await _service.SendManualReminderAsync(invoiceId);

        var history = await _service.GetReminderHistoryAsync(invoiceId);

        Assert.Equal(2, history.Count);
        Assert.Equal(r2.Id, history[0].Id);
        Assert.Equal(r1.Id, history[1].Id);
    }

    [Fact]
    public async Task GetReminderHistoryAsync_OnlyReturnsRemindersInCurrentWorkspace()
    {
        var (invoiceId, _) = await CreateOverdueInvoiceAsync("ws1@example.com");

        await _service.SendManualReminderAsync(invoiceId);

        var otherWsReminder = new Reminder(
            Guid.NewGuid(), invoiceId, ReminderType.ManualOverdue, ReminderChannel.Email,
            "other@example.com", "Other Subject", ReminderStatus.Sent);
        await _reminderRepository.AddAsync(otherWsReminder);

        var history = await _service.GetReminderHistoryAsync(invoiceId);

        Assert.Single(history);
        Assert.Equal("ws1@example.com", history[0].RecipientEmail);
    }

    [Fact]
    public async Task GetReminderHistoryAsync_WhenNone_ReturnsEmpty()
    {
        var (invoiceId, _) = await CreateOverdueInvoiceAsync();

        var history = await _service.GetReminderHistoryAsync(invoiceId);

        Assert.Empty(history);
    }

    [Fact]
    public async Task SendManualReminderAsync_IssuedInvoice_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, issueDate.AddDays(30), "USD");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendManualReminderAsync(invoice.Id));
    }

    [Fact]
    public async Task SendManualReminderAsync_PaidInvoice_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, issueDate.AddDays(30), "USD");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        invoice.MarkPaid();
        await _invoiceRepository.AddAsync(invoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendManualReminderAsync(invoice.Id));
    }

    [Fact]
    public async Task SendManualReminderAsync_CancelledInvoice_Throws()
    {
        var client = new Client(TestWorkspaceId, "Test Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-001", issueDate, issueDate.AddDays(30), "USD");
        invoice.Cancel();
        await _invoiceRepository.AddAsync(invoice);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.SendManualReminderAsync(invoice.Id));
    }
}

public class FakeReminderRepository : IReminderRepository
{
    public List<Reminder> All { get; } = [];

    public Guid? FilterWorkspaceId { get; set; }

    public Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        All.Add(reminder);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Reminder>> ListByInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var query = All.Where(r => r.InvoiceId == invoiceId);
        if (FilterWorkspaceId.HasValue)
            query = query.Where(r => r.WorkspaceId == FilterWorkspaceId.Value);
        return Task.FromResult<IReadOnlyList<Reminder>>(query.ToList().AsReadOnly());
    }
}

public class FakeEmailSender : IEmailSender
{
    public bool ShouldSucceed { get; set; } = true;

    public Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default) =>
        Task.FromResult(ShouldSucceed);
}

public class ClientNoEmailRepo : IClientRepository
{
    private readonly IClientRepository _inner;
    private readonly Guid _targetId;

    public ClientNoEmailRepo(IClientRepository inner, Guid targetId)
    {
        _inner = inner;
        _targetId = targetId;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _inner.GetByIdAsync(id, cancellationToken);
        if (client is not null && client.Id == _targetId)
        {
            var field = typeof(Client).GetField("<Email>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(client, null);
        }
        return client;
    }

    public async Task<Client?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken);

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default) =>
        await _inner.AddAsync(client, cancellationToken);

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
        await _inner.ListAsync(cancellationToken);
}

public class ClientEmptyEmailRepo : IClientRepository
{
    private readonly IClientRepository _inner;
    private readonly Guid _targetId;

    public ClientEmptyEmailRepo(IClientRepository inner, Guid targetId)
    {
        _inner = inner;
        _targetId = targetId;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _inner.GetByIdAsync(id, cancellationToken);
        if (client is not null && client.Id == _targetId)
        {
            var field = typeof(Client).GetField("<Email>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            field?.SetValue(client, "");
        }
        return client;
    }

    public async Task<Client?> GetByIdUnfilteredAsync(Guid id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken);

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default) =>
        await _inner.AddAsync(client, cancellationToken);

    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
        await _inner.ListAsync(cancellationToken);
}


