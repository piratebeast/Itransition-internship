using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace UserManagement.Services;

public class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailBackgroundService> _logger;
    private readonly IHttpClientFactory _httpFactory;

    public EmailBackgroundService(
        IEmailQueue queue,
        IConfiguration config,
        ILogger<EmailBackgroundService> logger,
        IHttpClientFactory httpFactory)
    {
        _queue = queue;
        _config = config;
        _logger = logger;
        _httpFactory = httpFactory;
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
                _logger.LogError(ex, "Failed to send email to {To}", job.To);
            }
        }
    }

    private async Task SendAsync(EmailJob job, CancellationToken ct)
    {
        var apiKey = _config["Email:ApiKey"]!;
        var from = _config["Email:From"]!;

        var payload = new
        {
            from,
            to = new[] { job.To },
            subject = job.Subject,
            html = job.HtmlBody
        };

        var client = _httpFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Resend returned {(int)response.StatusCode}: {body}");
        }
    }
}