using Microsoft.AspNetCore.Identity.UI.Services;

namespace UserManagement.Services;

// Logs the confirmation link so you can click it during development.
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger) => _logger = logger;

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        _logger.LogWarning("EMAIL to {Email} | {Subject}\n{Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}