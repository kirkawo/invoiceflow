using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Clients;
using InvoiceFlow.Domain;

namespace InvoiceFlow.UnitTests;

public class ClientServiceTests
{
    private static readonly Guid TestWorkspaceId = Guid.NewGuid();
    private readonly ClientService _service;
    private readonly FakeClientRepository _repository;

    public ClientServiceTests()
    {
        _repository = new FakeClientRepository();
        _service = new ClientService(_repository, new FakeCurrentWorkspaceService());
    }

    [Fact]
    public async Task CreateClientAsync_CreatesClient_AndReturnsId()
    {
        var id = await _service.CreateClientAsync("Alice", "alice@example.com", "Acme Inc.");

        var stored = await _repository.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Equal("Alice", stored.Name);
        Assert.Equal("alice@example.com", stored.Email);
        Assert.Equal("Acme Inc.", stored.CompanyName);
    }

    [Fact]
    public async Task CreateClientAsync_WithNullCompanyName_CreatesClient()
    {
        var id = await _service.CreateClientAsync("Bob", "bob@example.com", null);

        var stored = await _repository.GetByIdAsync(id);
        Assert.NotNull(stored);
        Assert.Null(stored.CompanyName);
    }

    [Fact]
    public async Task GetClientsAsync_ReturnsAllClients()
    {
        await _service.CreateClientAsync("Alice", "alice@example.com", null);
        await _service.CreateClientAsync("Bob", "bob@example.com", null);

        var clients = await _service.GetClientsAsync();

        Assert.Equal(2, clients.Count);
        Assert.Contains(clients, c => c.Name == "Alice");
        Assert.Contains(clients, c => c.Name == "Bob");
    }

    [Fact]
    public async Task GetClientsAsync_WhenEmpty_ReturnsEmptyList()
    {
        var clients = await _service.GetClientsAsync();

        Assert.Empty(clients);
    }

    [Fact]
    public async Task GetClientsAsync_MapsAllProperties()
    {
        var id = await _service.CreateClientAsync("Alice", "alice@example.com", "Acme");

        var clients = await _service.GetClientsAsync();
        var dto = clients.Single();

        Assert.Equal(id, dto.Id);
        Assert.Equal("Alice", dto.Name);
        Assert.Equal("alice@example.com", dto.Email);
        Assert.Equal("Acme", dto.CompanyName);
        Assert.False(dto.IsArchived);
    }
}

public class FakeClientRepository : IClientRepository
{
    private readonly Dictionary<Guid, Client> _store = [];

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryGetValue(id, out var client) ? client : null);

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        _store[client.Id] = client;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Client>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Client>>(_store.Values.ToList().AsReadOnly());
}

public class FakeCurrentWorkspaceService : ICurrentWorkspaceService
{
    public Guid WorkspaceId { get; set; } = Guid.NewGuid();
    public void SetWorkspaceId(Guid id) => WorkspaceId = id;
}
