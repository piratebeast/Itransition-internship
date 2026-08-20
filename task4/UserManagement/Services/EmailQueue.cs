using System.Threading.Channels;

namespace UserManagement.Services;

public record EmailJob(string To, string Subject, string HtmlBody);

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailJob job);
    IAsyncEnumerable<EmailJob> DequeueAllAsync(CancellationToken ct);
}

public class EmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _channel = Channel.CreateUnbounded<EmailJob>();

    public ValueTask EnqueueAsync(EmailJob job) => _channel.Writer.WriteAsync(job);

    public IAsyncEnumerable<EmailJob> DequeueAllAsync(CancellationToken ct)
        => _channel.Reader.ReadAllAsync(ct);
}