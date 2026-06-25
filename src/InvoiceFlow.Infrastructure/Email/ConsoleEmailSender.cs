using InvoiceFlow.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.Email;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Email sent (stub): To={To}, Subject={Subject}, Body={Body}", to, subject, body);
        return Task.FromResult(true);
    }
}
