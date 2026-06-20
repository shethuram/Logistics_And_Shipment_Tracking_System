using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Logistics.Api.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Logistics.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var host = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(_config["SmtpSettings:Port"] ?? "587");
        var enableSsl = bool.Parse(_config["SmtpSettings:EnableSsl"] ?? "true");
        var username = _config["SmtpSettings:Username"]!;
        var password = _config["SmtpSettings:Password"]!;

        _logger.LogInformation("Sending SMTP email to {ToEmail} via {Host}:{Port}...", toEmail, host, port);

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };

        using var mailMessage = new MailMessage
        {
            From = new MailAddress(username),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        mailMessage.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("SMTP email successfully sent to {ToEmail}.", toEmail);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Failed to send SMTP email to {ToEmail}.", toEmail);
            throw;
        }
    }
}
