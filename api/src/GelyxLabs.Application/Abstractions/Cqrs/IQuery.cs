namespace GelyxLabs.Application.Abstractions.Cqrs;

using GelyxLabs.Domain.Common;

public interface IQuery<TResponse> : IRequest<Result<TResponse>> { }
