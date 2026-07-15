using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class PublicInvoiceServiceTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();

    private readonly FakeInvoiceRepository _invoiceRepository;
    private readonly FakeClientRepository _clientRepository;
    private readonly PublicInvoiceService _service;

    public PublicInvoiceServiceTests()
    {
        _invoiceRepository = new FakeInvoiceRepository { FilterWorkspaceId = TestWorkspaceId };
        _clientRepository = new FakeClientRepository();
        _service = new PublicInvoiceService(_invoiceRepository, _clientRepository);
    }

    private async Task<(Guid invoiceId, string publicId)> CreateIssuedInvoiceAsync(
        string currency = "USD", int lineItemCount = 1)
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-TEST-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), currency);
        for (var i = 0; i < lineItemCount; i++)
            invoice.AddLineItem($"Item {i + 1}", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        return (invoice.Id, invoice.PublicId);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_NullPublicId_ReturnsNull()
    {
        var result = await _service.GetPublicInvoiceAsync(null!);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_EmptyPublicId_ReturnsNull()
    {
        var result = await _service.GetPublicInvoiceAsync("");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_WhitespacePublicId_ReturnsNull()
    {
        var result = await _service.GetPublicInvoiceAsync("   ");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_InvalidPublicId_ReturnsNull()
    {
        var result = await _service.GetPublicInvoiceAsync("nonexistent-token-xyz");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_DraftInvoice_ReturnsNull()
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-DRAFT-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), "USD");
        invoice.AddLineItem("Item", 1, 100);
        await _invoiceRepository.AddAsync(invoice);

        var result = await _service.GetPublicInvoiceAsync(invoice.PublicId);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_CancelledInvoice_ReturnsNull()
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-CANC-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), "USD");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        invoice.Cancel();
        await _invoiceRepository.AddAsync(invoice);

        var result = await _service.GetPublicInvoiceAsync(invoice.PublicId);
        Assert.Null(result);
    }

    [Fact]
    public void DomainPreventsIssuingInvoiceWithoutLineItems()
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-NOLI-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), "USD");

        Assert.Throws<InvalidOperationException>(() => invoice.Issue());
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_ValidIssuedInvoice_ReturnsDto()
    {
        var (invoiceId, publicId) = await CreateIssuedInvoiceAsync();

        var result = await _service.GetPublicInvoiceAsync(publicId);

        Assert.NotNull(result);
        Assert.Equal("INV-TEST-001", result.Number);
        Assert.Equal(InvoiceStatus.Issued, result.Status);
        Assert.Equal("USD", result.Currency);
        Assert.Single(result.LineItems);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_ValidOverdueInvoice_ReturnsDto()
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-OVERDUE-001",
            DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(-1), "EUR");
        invoice.AddLineItem("Item", 1, 250);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        var result = await _service.GetPublicInvoiceAsync(invoice.PublicId);

        Assert.NotNull(result);
        Assert.Equal(InvoiceStatus.Issued, result.Status);
        Assert.Equal("EUR", result.Currency);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_DoesNotFilterByWorkspace()
    {
        var otherWorkspaceId = Guid.NewGuid();
        var client = new Client(otherWorkspaceId, "Other Client", "other@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(otherWorkspaceId, client.Id, "INV-OTHER-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), "GBP");
        invoice.AddLineItem("Item", 1, 100);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        var result = await _service.GetPublicInvoiceAsync(invoice.PublicId);

        Assert.NotNull(result);
        Assert.Equal("INV-OTHER-001", result.Number);
        Assert.Equal("GBP", result.Currency);
    }

    [Fact]
    public async Task GetPublicInvoiceAsync_IncludesLineItemDetails()
    {
        var client = new Client(TestWorkspaceId, "Client", "client@example.com");
        await _clientRepository.AddAsync(client);

        var invoice = new Invoice(TestWorkspaceId, client.Id, "INV-LINE-001",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), "USD");
        invoice.AddLineItem("Consulting", 10, 150);
        invoice.AddLineItem("Expenses", 1, 500);
        invoice.Issue();
        await _invoiceRepository.AddAsync(invoice);

        var result = await _service.GetPublicInvoiceAsync(invoice.PublicId);

        Assert.NotNull(result);
        Assert.Equal(2, result.LineItems.Count);
        Assert.Equal(2000m, result.Total);
        Assert.Contains(result.LineItems, li => li.Description == "Consulting" && li.Quantity == 10 && li.UnitPrice == 150);
        Assert.Contains(result.LineItems, li => li.Description == "Expenses" && li.Quantity == 1 && li.UnitPrice == 500);
    }
}
