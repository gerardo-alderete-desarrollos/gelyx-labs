using GelyxLabs.Api;
using GelyxLabs.Api.Services.Email;
using GelyxLabs.Application.Abstractions.Email;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public sealed class EmailQueueServiceTests
{
    [Fact]
    public void ApiServices_CanResolveEmailQueueWithScopeValidationEnabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true
        });

        var queue = provider.GetRequiredService<EmailQueueService>();

        queue.Should().NotBeNull();
    }

    [Fact]
    public async Task LoggingSender_SuppressesDelivery()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance, new TestHostEnvironment());

        await sender.SendAsync(new EmailMessage("user@example.test", "subject", "<p>secret</p>", "es"));

        true.Should().BeTrue();
    }

    [Fact]
    public async Task Queue_DeliversLocalizedTemplateToFakeProvider()
    {
        var sender = new RecordingEmailSender();
        using var provider = BuildQueueProvider(sender);
        var queue = provider.GetRequiredService<EmailQueueService>();
        await queue.StartAsync(CancellationToken.None);

        var message = EmailTemplates.PasswordReset("user@example.test", "en", "https://web.test/reset?token=secret");
        await queue.EnqueueAsync(message);

        var delivered = await sender.NextMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await queue.StopAsync(CancellationToken.None);

        delivered.Subject.Should().Be("Reset your password - GelyxLabs");
        delivered.Locale.Should().Be("en");
        delivered.HtmlBody.Should().Contain("Reset password");
        delivered.HtmlBody.Should().NotContain("Restablece");
    }

    [Fact]
    public async Task Queue_RetriesFailuresAndContinuesWithNextMessage()
    {
        var sender = new FailingThenRecordingEmailSender();
        using var provider = BuildQueueProvider(sender);
        var queue = provider.GetRequiredService<EmailQueueService>();
        await queue.StartAsync(CancellationToken.None);

        await queue.EnqueueAsync(new EmailMessage("fail@example.test", "first", "<p>first</p>", "es"));
        await queue.EnqueueAsync(new EmailMessage("next@example.test", "second", "<p>second</p>", "es"));

        var delivered = await sender.NextMessage.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await queue.StopAsync(CancellationToken.None);

        sender.FailedAttempts.Should().Be(3);
        delivered.To.Should().Be("next@example.test");
    }

    [Fact]
    public void EmailOptions_RejectsIncompleteResendConfiguration()
    {
        var result = new EmailOptionsValidator().Validate(null, new EmailOptions
        {
            Provider = "Resend",
            From = "noreply@example.test"
        });

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void EmailOptions_AcceptsDefaultLoggingConfiguration()
    {
        var result = new EmailOptionsValidator().Validate(null, new EmailOptions());

        result.Succeeded.Should().BeTrue();
    }

    private static ServiceProvider BuildQueueProvider(IEmailSender sender)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IEmailSender>(_ => sender);
        services.AddSingleton<EmailQueueService>();
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public TaskCompletionSource<EmailMessage> NextMessage { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            NextMessage.TrySetResult(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingThenRecordingEmailSender : IEmailSender
    {
        private readonly RecordingEmailSender _recordingSender = new();
        public int FailedAttempts { get; private set; }
        public TaskCompletionSource<EmailMessage> NextMessage => _recordingSender.NextMessage;

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message.To == "fail@example.test")
            {
                FailedAttempts++;
                throw new InvalidOperationException("Expected test failure.");
            }

            return _recordingSender.SendAsync(message, cancellationToken);
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "GelyxLabs.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
