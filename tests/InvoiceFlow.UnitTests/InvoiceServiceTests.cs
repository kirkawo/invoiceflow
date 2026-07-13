using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.UnitTests;

public class InvoiceServiceTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();
    private readonly InvoiceService _service;
    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;

    public InvoiceServiceTests()
    {
        _invoiceRepository = new FakeInvoiceRepository { FilterWorkspaceId = TestWorkspaceId };
        _clientRepository = new FakeClientRepository();
        _service = new InvoiceService(_invoiceRepository, _clientRepository, new FakeCurrentWorkspaceService { WorkspaceId = TestWorkspaceId });
    }

    private async Task<Guid> CreateClientAsync(string name = "Client")
    {
        var client = new Client(TestWorkspaceId, name, $"{name.ToLower()}@example.com");
        await _clientRepository.AddAsync(client);
        return client.Id;
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_CreatesInvoice_AndReturnsId()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);

        var id = await _service.CreateInvoiceDraftAsync(clientId, "INV-001", issueDate, dueDate, "USD", null);

        var stored = await _invoiceRepository.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Equal("INV-001", stored.Number);
        Assert.Equal(InvoiceStatus.Draft, stored.Status);
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_Throws_WhenClientNotFound()
    {
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateInvoiceDraftAsync(Guid.NewGuid(), "INV-001", issueDate, issueDate.AddDays(30), "USD", null));
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_Throws_WhenClientIdIsEmpty()
    {
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateInvoiceDraftAsync(Guid.Empty, "INV-001", issueDate, issueDate.AddDays(30), "USD", null));
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_AutoGeneratesNumber_WhenNumberIsEmpty()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        var id = await _service.CreateInvoiceDraftAsync(clientId, "", issueDate, issueDate.AddDays(30), "USD", null);

        var stored = await _invoiceRepository.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Matches(@"^INV-\d{4}-0{3}\d$", stored.Number);
        Assert.NotEmpty(stored.Number);
    }

    [Fact]
    public async Task GetInvoiceAsync_ReturnsMappedDto()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await _service.CreateInvoiceDraftAsync(clientId, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);

        var dto = await _service.GetInvoiceAsync(invoiceId);

        Assert.NotNull(dto);
        Assert.Equal(invoiceId, dto.Id);
        Assert.Equal(clientId, dto.ClientId);
        Assert.Equal("INV-001", dto.Number);
        Assert.Equal(issueDate, dto.IssueDateUtc);
        Assert.Equal(InvoiceStatus.Draft, dto.Status);
        Assert.Equal("USD", dto.Currency);
        Assert.Equal(0, dto.Total);
    }

    [Fact]
    public async Task GetInvoiceAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _service.GetInvoiceAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetClientInvoicesAsync_ReturnsInvoicesForClient()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _service.CreateInvoiceDraftAsync(clientId, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(clientId, "INV-002", issueDate, issueDate.AddDays(30), "EUR", null);

        var invoices = await _service.GetClientInvoicesAsync(clientId);

        Assert.Equal(2, invoices.Count);
    }

    [Fact]
    public async Task GetClientInvoicesAsync_ReturnsOnlySpecifiedClient()
    {
        var client1Id = await CreateClientAsync("Client A");
        var client2Id = await CreateClientAsync("Client B");
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _service.CreateInvoiceDraftAsync(client1Id, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(client2Id, "INV-002", issueDate, issueDate.AddDays(30), "USD", null);

        var invoices = await _service.GetClientInvoicesAsync(client1Id);

        Assert.Single(invoices);
        Assert.Equal("INV-001", invoices[0].Number);
    }

    [Fact]
    public async Task GetClientInvoicesAsync_WhenNone_ReturnsEmptyList()
    {
        var clientId = await CreateClientAsync();

        var invoices = await _service.GetClientInvoicesAsync(clientId);

        Assert.Empty(invoices);
    }

    [Fact]
    public async Task AddLineItemAsync_AddsLineItemAndReturnsId()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);

        await _service.AddLineItemAsync(invoiceId, "Consulting", 10, 100);

        var dto = await _service.GetInvoiceAsync(invoiceId);
        Assert.NotNull(dto);
        Assert.Single(dto.LineItems);
        Assert.Equal(1000, dto.Total);
    }

    [Fact]
    public async Task AddLineItemAsync_Throws_WhenInvoiceNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.AddLineItemAsync(Guid.NewGuid(), "Item", 1, 100));
    }

    [Fact]
    public async Task UpdateLineItemAsync_UpdatesLineItem()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        var lineItemId = await _service.AddLineItemAsync(invoiceId, "Consulting", 10, 100);

        await _service.UpdateLineItemAsync(invoiceId, lineItemId, "Premium Consulting", 5, 200);

        var dto = await _service.GetInvoiceAsync(invoiceId);
        Assert.NotNull(dto);
        Assert.Single(dto.LineItems);
        Assert.Equal("Premium Consulting", dto.LineItems[0].Description);
        Assert.Equal(1000, dto.Total);
    }

    [Fact]
    public async Task RemoveLineItemAsync_RemovesLineItem()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        await _service.AddLineItemAsync(invoiceId, "Item", 2, 50);

        await _service.RemoveLineItemAsync(invoiceId, 0);

        var dto = await _service.GetInvoiceAsync(invoiceId);
        Assert.NotNull(dto);
        Assert.Empty(dto.LineItems);
        Assert.Equal(0, dto.Total);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_ReturnsAllInvoices()
    {
        var client1Id = await CreateClientAsync("Client A");
        var client2Id = await CreateClientAsync("Client B");
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _service.CreateInvoiceDraftAsync(client1Id, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(client2Id, "INV-002", issueDate, issueDate.AddDays(30), "USD", null);

        var invoices = await _service.GetAllInvoicesAsync();

        Assert.Equal(2, invoices.Count);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_FiltersByClient()
    {
        var client1Id = await CreateClientAsync("Client A");
        var client2Id = await CreateClientAsync("Client B");
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await _service.CreateInvoiceDraftAsync(client1Id, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(client2Id, "INV-002", issueDate, issueDate.AddDays(30), "USD", null);

        var invoices = await _service.GetAllInvoicesAsync(client1Id);

        Assert.Single(invoices);
        Assert.Equal("INV-001", invoices[0].Number);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_FiltersByStatus()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await _service.CreateInvoiceDraftAsync(clientId, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(clientId, "INV-002", issueDate, issueDate.AddDays(30), "EUR", null);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var invoices = await _service.GetAllInvoicesAsync(status: InvoiceStatus.Issued);

        Assert.Single(invoices);
        Assert.Equal("INV-001", invoices[0].Number);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_FiltersByClientAndStatus()
    {
        var client1Id = await CreateClientAsync("Client A");
        var client2Id = await CreateClientAsync("Client B");
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var inv1Id = await _service.CreateInvoiceDraftAsync(client1Id, "INV-001", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.CreateInvoiceDraftAsync(client2Id, "INV-002", issueDate, issueDate.AddDays(30), "USD", null);
        await _service.AddLineItemAsync(inv1Id, "Item", 1, 100);
        await _service.IssueInvoiceAsync(inv1Id);

        var invoices = await _service.GetAllInvoicesAsync(client1Id, InvoiceStatus.Issued);

        Assert.Single(invoices);
        Assert.Equal("INV-001", invoices[0].Number);
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_AutoGeneratesNumber_WhenNumberIsNull()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);

        var id = await _service.CreateInvoiceDraftAsync(clientId, null, issueDate, dueDate, "USD", null);

        var stored = await _invoiceRepository.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Matches(@"^INV-\d{4}-0{3}\d$", stored.Number);
        Assert.NotEmpty(stored.Number);
        Assert.Equal(InvoiceStatus.Draft, stored.Status);
    }

    [Fact]
    public async Task CreateInvoiceDraftAsync_AutoGeneratedNumbers_AreSequential()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var dueDate = issueDate.AddDays(30);

        var id1 = await _service.CreateInvoiceDraftAsync(clientId, null, issueDate, dueDate, "USD", null);
        var id2 = await _service.CreateInvoiceDraftAsync(clientId, null, issueDate, dueDate, "USD", null);

        var inv1 = await _invoiceRepository.GetByIdAsync(id1);
        var inv2 = await _invoiceRepository.GetByIdAsync(id2);
        Assert.NotNull(inv1);
        Assert.NotNull(inv2);
        Assert.NotEqual(inv1.Number, inv2.Number);
        Assert.Matches(@"^INV-\d{4}-0{3}\d$", inv1.Number);
    }

    [Fact]
    public async Task GetAllInvoicesAsync_WhenNone_ReturnsEmptyList()
    {
        var invoices = await _service.GetAllInvoicesAsync();
        Assert.Empty(invoices);
    }

    [Fact]
    public async Task PublicFlow_RoundTrip_ByPublicId()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        await _service.AddLineItemAsync(invoiceId, "Consulting", 10, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var dto = await _service.GetInvoiceAsync(invoiceId);
        Assert.NotNull(dto);
        Assert.NotEmpty(dto.PublicId);

        var publicService = new PublicInvoiceService(_invoiceRepository, _clientRepository);
        var publicDto = await publicService.GetPublicInvoiceAsync(dto.PublicId);

        Assert.NotNull(publicDto);
        Assert.Equal(dto.Number, publicDto.Number);
    }

    #region Overdue sync tests

    [Fact]
    public async Task SyncOverdueStatus_IssuedInvoiceDueBeforeToday_BecomesOverdue()
    {
        var clientId = await CreateClientAsync();
        var pastDue = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await CreateDraftInvoiceAsync(clientId, pastDue);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var count = await syncService.SyncOverdueStatusAsync(now);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(1, count);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_IssuedInvoiceDueToday_StaysIssued()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var today = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await syncService.SyncOverdueStatusAsync(today);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(0, count);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Issued, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_PaidInvoice_StaysPaid()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);
        await _service.MarkInvoicePaidAsync(invoiceId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var future = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await syncService.SyncOverdueStatusAsync(future);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(0, count);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_CancelledInvoice_StaysCancelled()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);
        await _service.CancelInvoiceAsync(invoiceId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var future = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await syncService.SyncOverdueStatusAsync(future);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(0, count);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Cancelled, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_DraftInvoice_StaysDraft()
    {
        var clientId = await CreateClientAsync();
        var invoiceId = await CreateDraftInvoiceAsync(clientId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var future = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var count = await syncService.SyncOverdueStatusAsync(future);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(0, count);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Draft, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_Idempotent_RunningTwiceDoesNotBreak()
    {
        var clientId = await CreateClientAsync();
        var pastDue = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoiceId = await CreateDraftInvoiceAsync(clientId, pastDue);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var count1 = await syncService.SyncOverdueStatusAsync(now);
        var count2 = await syncService.SyncOverdueStatusAsync(now);

        var invoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.Equal(1, count1);
        Assert.Equal(0, count2);
        Assert.NotNull(invoice);
        Assert.Equal(InvoiceStatus.Overdue, invoice.Status);
    }

    [Fact]
    public async Task SyncOverdueStatus_RespectsWorkspaceIsolation()
    {
        var clientId = await CreateClientAsync();
        var pastDue = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var now = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        var invoiceId = await CreateDraftInvoiceAsync(clientId, pastDue);
        await _service.AddLineItemAsync(invoiceId, "Item", 1, 100);
        await _service.IssueInvoiceAsync(invoiceId);

        var otherWsId = Guid.NewGuid();
        var otherInvoice = new Invoice(otherWsId, clientId, "INV-OTHER", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), pastDue, "USD");
        otherInvoice.AddLineItem("Item", 1, 100);
        otherInvoice.Issue();
        await _invoiceRepository.AddAsync(otherInvoice);

        var syncService = new InvoiceStatusSyncService(_invoiceRepository, TestNullLogger<InvoiceStatusSyncService>.Instance);
        var count = await syncService.SyncOverdueStatusAsync(now);

        Assert.Equal(1, count);

        var syncedInvoice = await _invoiceRepository.GetByIdAsync(invoiceId);
        Assert.NotNull(syncedInvoice);
        Assert.Equal(InvoiceStatus.Overdue, syncedInvoice.Status);

        var otherInDb = await _invoiceRepository.GetByIdAsync(otherInvoice.Id);
        Assert.NotNull(otherInDb);
        Assert.Equal(InvoiceStatus.Issued, otherInDb.Status);
    }

    #endregion

    private async Task<Guid> CreateDraftInvoiceAsync(Guid clientId, DateTime? dueDate = null)
    {
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        return await _service.CreateInvoiceDraftAsync(clientId, "INV-001", issueDate, dueDate ?? issueDate.AddDays(30), "USD", null);
    }
}

public class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<Guid, Invoice> _store = [];
    public Guid? FilterWorkspaceId { get; set; }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

    public Task<Invoice?> GetByPublicIdAsync(string publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.Values.FirstOrDefault(i => i.PublicId == publicId));

    public Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _store[invoice.Id] = invoice;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Invoice>> ListByClientAsync(Guid clientId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values.Where(i => i.ClientId == clientId).ToList().AsReadOnly());

    public Task<IReadOnlyList<Invoice>> ListAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Invoice>>(
            _store.Values.ToList().AsReadOnly());

    public Task<IReadOnlyList<Invoice>> GetOverdueCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        var query = _store.Values.AsEnumerable();
        if (FilterWorkspaceId.HasValue)
            query = query.Where(i => i.WorkspaceId == FilterWorkspaceId.Value);
        return Task.FromResult<IReadOnlyList<Invoice>>(
            query.Where(i => i.Status == InvoiceStatus.Issued && i.DueDateUtc < utcNow)
                .ToList()
                .AsReadOnly());
    }

    public Task<string> GetNextInvoiceNumberAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";
        var maxSeq = _store.Values
            .Where(i => i.Number.StartsWith(prefix))
            .Select(i => int.TryParse(i.Number[prefix.Length..], out var seq) ? seq : 0)
            .DefaultIfEmpty(0)
            .Max();
        return Task.FromResult($"{prefix}{(maxSeq + 1):D4}");
    }
}
