namespace GelyxLabs.Api.Extensions;

using GelyxLabs.Api.Common;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpointGroups(this IServiceCollection services, params Type[] assemblyMarkerTypes)
    {
        var assemblies = assemblyMarkerTypes.Length == 0
            ? new[] { typeof(Program).Assembly }
            : assemblyMarkerTypes.Select(t => t.Assembly);

        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && typeof(IEndpointGroup).IsAssignableFrom(t));
            foreach (var type in types)
            {
                services.AddTransient(typeof(IEndpointGroup), type);
            }
        }
        return services;
    }

    public static WebApplication MapEndpointGroups(this WebApplication app, IEndpointRouteBuilder routeBuilder)
    {
        var groups = app.Services.GetServices<IEndpointGroup>();
        foreach (var group in groups)
        {
            group.MapEndpoints(routeBuilder);
        }
        return app;
    }
}
