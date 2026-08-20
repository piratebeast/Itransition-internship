using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace UserManagement.Services;

public class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(IEmailQueue queue, IConfiguration config,
        ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await SendAsync(job, stoppingToken);
                _logger.LogInformation("Sent email to {To}", job.To);
            }
            catch (Exception ex)
            {
                // A failed email must never take down the worker, or every
                // subsequent registration would silently stop sending.
                _logger.LogError(ex, "Failed to send email to {To}", job.To);
            }
        }
    }

    private async Task SendAsync(EmailJob job, CancellationToken ct)
    {
        var host = _config["Email:Host"]!;
        var port = int.Parse(_config["Email:Port"]!);
        var user = _config["Email:User"]!;
        var pass = _config["Email:Password"]!;
        var from = _config["Email:From"] ?? user;

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(job.To));
        message.Subject = job.Subject;
        message.Body = new TextPart("html") { Text = job.HtmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(user, pass, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}