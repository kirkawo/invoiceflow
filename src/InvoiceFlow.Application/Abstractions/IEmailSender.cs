namespace InvoiceFlow.Application.Abstractions;

public interface IEmailSender
{
    Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}
