namespace InvoiceFlow.Domain;

public class Client
{
    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string? CompanyName { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public bool IsArchived { get; private set; }

    private Client() => Name = Email = null!;

    public Client(Guid workspaceId, string name, string email, string? companyName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Id = Guid.NewGuid();
        WorkspaceId = workspaceId;
        Name = name;
        Email = email;
        CompanyName = companyName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Archive() => IsArchived = true;
    public void Restore() => IsArchived = false;
}
