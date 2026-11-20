namespace GelyxLabs.Application.Abstractions.Dispatching;

using GelyxLabs.Application.Abstractions.Cqrs;
using GelyxLabs.Domain.Common;
using Microsoft.Extensions.DependencyInjection;

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public Dispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand>.Handle));
        return await (Task<Result>)method!.Invoke(handler, [command, cancellationToken])!;
    }

    public async Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResponse>, TResponse>.Handle));
        return await (Task<Result<TResponse>>)method!.Invoke(handler, [command, cancellationToken])!;
    }

    public async Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var method = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResponse>, TResponse>.Handle));
        return await (Task<Result<TResponse>>)method!.Invoke(handler, [query, cancellationToken])!;
    }
}
