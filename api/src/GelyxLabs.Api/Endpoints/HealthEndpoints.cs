namespace GelyxLabs.Api.Endpoints;

using GelyxLabs.Api.Common;

public class HealthEndpoints : EndpointGroupBase
{
    public override void MapEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("health").WithTags("Health");
        group.MapGet("/", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));
    }
}
