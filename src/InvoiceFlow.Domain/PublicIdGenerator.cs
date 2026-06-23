namespace InvoiceFlow.Domain;

public static class PublicIdGenerator
{
    public static string New() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
