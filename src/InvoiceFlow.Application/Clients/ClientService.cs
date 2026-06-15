using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Domain;

namespace InvoiceFlow.Application.Clients;

public class ClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly ICurrentWorkspaceService _workspaceService;

    public ClientService(IClientRepository clientRepository, ICurrentWorkspaceService workspaceService)
    {
        _clientRepository = clientRepository;
        _workspaceService = workspaceService;
    }

    public async Task<Guid> CreateClientAsync(
        string name,
        string email,
        string? companyName,
        CancellationToken cancellationToken = default)
    {
        var client = new Client(_workspaceService.WorkspaceId, name, email, companyName);
        await _clientRepository.AddAsync(client, cancellationToken);
        return client.Id;
    }

    public async Task<IReadOnlyList<ClientDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.ListAsync(cancellationToken);
        return clients.Select(MapToDto).ToList().AsReadOnly();
    }

    private static ClientDto MapToDto(Client client) =>
        new()
        {
            Id = client.Id,
            Name = client.Name,
            Email = client.Email,
            CompanyName = client.CompanyName,
            CreatedAtUtc = client.CreatedAtUtc,
            IsArchived = client.IsArchived
        };
}
