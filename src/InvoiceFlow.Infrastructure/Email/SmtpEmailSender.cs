using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace InvoiceFlow.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailOptions options, ILogger<SmtpEmailSender> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            {
                _logger.LogWarning("SMTP not configured — email not sent. To={To}, Subject={Subject}", to, subject);
                return false;
            }

            using var client = new SmtpClient();

            await client.ConnectAsync(_options.SmtpHost, _options.SmtpPort, SecureSocketOptions.StartTls, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent: To={To}, Subject={Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email: To={To}, Subject={Subject}, SmtpHost={SmtpHost}, SmtpPort={SmtpPort}",
                to, subject, _options.SmtpHost, _options.SmtpPort);
            return false;
        }
    }
}
