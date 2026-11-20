namespace GelyxLabs.Api.Common;

public abstract class EndpointGroupBase : IEndpointGroup
{
    public abstract void MapEndpoints(IEndpointRouteBuilder app);
}
