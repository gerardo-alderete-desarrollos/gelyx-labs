namespace GelyxLabs.Application.Abstractions.Cqrs;

using GelyxLabs.Domain.Common;

public interface ICommand : IRequest<Result> { }
public interface ICommand<TResponse> : IRequest<Result<TResponse>> { }
