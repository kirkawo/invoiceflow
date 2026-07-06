using System.Net;
using System.Net.Mail;
using InvoiceFlow.Application.Abstractions;
using InvoiceFlow.Application.Options;
using Microsoft.Extensions.Logging;

namespace InvoiceFlow.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _smtp;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(EmailOptions options, ILogger<SmtpEmailSender> logger)
    {
        _smtp = new SmtpOptions
        {
            Host = options.SmtpHost,
            Port = options.SmtpPort,
            FromAddress = options.FromAddress,
            FromName = options.FromName,
            Username = options.Username,
            Password = options.Password,
            UseSsl = options.UseSsl,
        };
        _logger = logger;
    }

    public async Task<bool> TrySendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_smtp.Host))
            {
                _logger.LogWarning("SMTP not configured — email not sent. To={To}, Subject={Subject}", to, subject);
                return false;
            }

            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
            };

            if (!string.IsNullOrWhiteSpace(_smtp.Username))
            {
                client.Credentials = new NetworkCredential(_smtp.Username, _smtp.Password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_smtp.FromAddress, _smtp.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };

            message.To.Add(to);

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent: To={To}, Subject={Subject}", to, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: To={To}, Subject={Subject}", to, subject);
            return false;
        }
    }

    private sealed record SmtpOptions
    {
        public string Host { get; init; } = string.Empty;
        public int Port { get; init; } = 587;
        public string FromAddress { get; init; } = string.Empty;
        public string FromName { get; init; } = string.Empty;
        public string Username { get; init; } = string.Empty;
        public string Password { get; init; } = string.Empty;
        public bool UseSsl { get; init; } = true;
    }
}
