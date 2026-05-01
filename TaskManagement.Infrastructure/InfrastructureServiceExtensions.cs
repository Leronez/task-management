using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Infrastructure.Messaging;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Infrastructure;

/// <summary>Extension methods for registering Infrastructure services into the DI container.</summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>Registers EF Core, repositories, application service, and messaging infrastructure.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default")));

        // IUnitOfWork is implemented by AppDbContext (same scoped instance)
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<ITaskService, TaskService>();

        // RabbitMQ publisher is Singleton — holds a persistent connection.
        // Register as both concrete type (for direct resolution) and interface (for OutboxProcessor DI).
        services.AddSingleton<RabbitMqPublisher>();
        services.AddSingleton<IMessagePublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());
        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}
