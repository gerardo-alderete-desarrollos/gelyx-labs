using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace GelyxLabs.Infrastructure.Services;

public class StartupInitializer : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupInitializer> _logger;
    private readonly TaskCompletionSource _completionSource = new();

    public StartupInitializer(IServiceProvider serviceProvider, ILogger<StartupInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task Completion => _completionSource.Task;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting application initialization...");

        using var scope = _serviceProvider.CreateScope();

        var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();
        await databaseInitializer.InitializeAsync(cancellationToken);

        var moduleSynchronizer = scope.ServiceProvider.GetRequiredService<IModuleSynchronizer>();
        try
        {
            await moduleSynchronizer.SynchronizeAsync(cancellationToken);
            _logger.LogInformation("Module synchronization completed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Module synchronization failed; schema may not exist. Will retry on next run if MIGRATE_ON_STARTUP is enabled.");
        }

        _logger.LogInformation("Application initialization completed");
        _completionSource.SetResult();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}