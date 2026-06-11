using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class InvoiceServiceTests
{
    private readonly InvoiceService _service;
    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;

    public InvoiceServiceTests()
    {
        _invoiceRepository = new FakeInvoiceRepository();
        _clientRepository = new FakeClientRepository();
        _service = new InvoiceService(_invoiceRepository, _clientRepository);
    }

    private async Task<Guid> CreateClientAsync(string name = "Client")
    {
        var client = new Client(name, $"{name.ToLower()}@example.com");
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
    public async Task CreateInvoiceDraftAsync_Throws_WhenNumberIsEmpty()
    {
        var clientId = await CreateClientAsync();
        var issueDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateInvoiceDraftAsync(clientId, "", issueDate, issueDate.AddDays(30), "USD", null));
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
}

public class FakeInvoiceRepository : IInvoiceRepository
{
    private readonly Dictionary<Guid, Invoice> _store = [];

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var invoice) ? invoice : null);

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
}
