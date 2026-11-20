namespace GelyxLabs.Api.Services.Email;

using System.Threading.Channels;
using GelyxLabs.Application.Abstractions.Email;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public sealed class EmailQueueService : IEmailQueue, IHostedService
{
    private readonly Channel<EmailMessage> _channel;
    private readonly ILogger<EmailQueueService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;

    public EmailQueueService(ILogger<EmailQueueService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _channel = Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _processingTask = Task.Run(() => ProcessQueueAsync(_cancellationTokenSource.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel.Writer.TryComplete();
        _cancellationTokenSource?.Cancel();
        if (_processingTask is not null)
        {
            await _processingTask.WaitAsync(cancellationToken);
        }
    }

    private async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken))
            {
                for (var attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                        await emailSender.SendAsync(message, cancellationToken);
                        break;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception ex) when (attempt < 3)
                    {
                        _logger.LogWarning(ex, "Email delivery attempt {Attempt} of {MaxAttempts} failed for {To}",
                            attempt, 3, message.To);
                        await Task.Delay(TimeSpan.FromMilliseconds(100 * (1 << (attempt - 1))), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Email delivery failed after {Attempts} attempts for {To}",
                            attempt, message.To);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }
}
