namespace InvoiceFlow.Application.Clients;

public class ClientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? CompanyName { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public bool IsArchived { get; init; }
}
