using MassTransit;
using SharedModels.Events;

namespace OrderService.Messaging;

/// <summary>
/// Configures MassTransit with RabbitMQ transport for OrderService.
/// Registers publisher endpoint for OrderPlacedEvent and OrderStatusChangedEvent,
/// and a consumer for PaymentCompletedEvent.
/// </summary>
public static class MassTransitConfig
{
    public static IServiceCollection AddOrderServiceMessaging(
        this IServiceCollection services, IConfiguration config)
    {
        var host = config["RabbitMQ:Host"];
        if (string.IsNullOrEmpty(host)) return services; // skip if not configured

        services.AddMassTransit(x =>
        {
            // Consumer: receives PaymentCompletedEvent from PaymentService
            x.AddConsumer<PaymentCompletedConsumer>();

            x.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(host, "/", h =>
                {
                    h.Username(config["RabbitMQ:Username"] ?? "guest");
                    h.Password(config["RabbitMQ:Password"] ?? "guest");
                });

                // Retry policy: 1s, 5s, 15s then dead-letter
                cfg.UseMessageRetry(r => r.Intervals(
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(15)));

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
