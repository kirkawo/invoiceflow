using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.Email;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger, EmailOptions options)
    {
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(options.SmtpHost))
            _logger.LogDebug("Email configured: {Host}:{Port} from {From}", options.SmtpHost, options.SmtpPort, options.FromAddress);
    }

    public Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Email (stub): To={To}, Subject={Subject}", to, subject);
        _logger.LogTrace("Email body: {Body}", body);
        return Task.FromResult(true);
    }
}
