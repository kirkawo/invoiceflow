namespace InvoiceFlow.Domain;

public class Workspace
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Workspace() => Name = null!;

    public Workspace(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Id = Guid.NewGuid();
        Name = name;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
