using Microsoft.AspNetCore.Identity.UI.Services;

namespace UserManagement.Services;

// Identity calls this synchronously during registration. Enqueueing returns
// immediately, so the HTTP response never waits on SMTP.
public class QueuedEmailSender : IEmailSender
{
    private readonly IEmailQueue _queue;

    public QueuedEmailSender(IEmailQueue queue) => _queue = queue;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        => await _queue.EnqueueAsync(new EmailJob(email, subject, htmlMessage));
}