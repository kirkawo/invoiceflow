namespace InvoiceFlow.Domain;

public class Client
{
    public Guid Id { get; }
    public string Name { get; }
    public string Email { get; }
    public string? CompanyName { get; }
    public DateTime CreatedAtUtc { get; }
    public bool IsArchived { get; private set; }

    public Client(string name, string email, string? companyName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        CompanyName = companyName;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Archive() => IsArchived = true;
    public void Restore() => IsArchived = false;
}
