namespace GelyxLabs.Application;

using GelyxLabs.Application.Abstractions.Dispatching;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddTransient<IDispatcher, Dispatcher>();
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(Abstractions.Cqrs.ICommandHandler<>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(Abstractions.Cqrs.ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(Abstractions.Cqrs.IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithTransientLifetime());
        services.Scan(scan => scan.FromAssemblies(assembly)
            .AddClasses(classes => classes.AssignableTo(typeof(IModuleDefinition)))
            .As<IModuleDefinition>()
            .WithTransientLifetime());
        return services;
    }
}
