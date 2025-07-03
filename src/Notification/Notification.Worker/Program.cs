using MassTransit;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Application.Settings;
using Notification.Infrastructure;
using Notification.Worker.Consumers;
using RabbitMQ.Client;

namespace Notification.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configuración de la base de datos
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<NotificationDbContext>(options =>
                options.UseNpgsql(connectionString));

            //Email Configuration 
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddTransient<IEmailService, EmailService>();

            // Worker en segundo plano
            builder.Services.AddHostedService<Worker>();

            // Configuración de MassTransit + RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                // Registrar consumidores
                x.AddConsumer<ProductCreatedConsumer>();
                x.AddConsumer<ProductUpdatedConsumer>();
                x.AddConsumer<ProductDeletedConsumer>();
                x.AddConsumer<ProductCreatedConsumerError>(); // Consumer para mensajes de error en la creación de productos

                // Configurar transporte RabbitMQ
                x.UsingRabbitMq((context, cfg) =>
                {
                    var rabbitHost = builder.Configuration["RabbitMQ:Host"];
                    var rabbitUser = builder.Configuration["RabbitMQ:Username"];
                    var rabbitPass = builder.Configuration["RabbitMQ:Password"];

                    cfg.Host(rabbitHost, "/", h =>
                    {
                        h.Username(rabbitUser);
                        h.Password(rabbitPass);
                    });

                    // Exchange y colas manualmente asociadas
                    const string exchangeName = "inventory_exchange";

                    // ---- Cola: producto creado ----
                    cfg.ReceiveEndpoint("product-created-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 10 reintentos cada 5s para probar
                        e.ConfigureConsumer<ProductCreatedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.created";
                        });
                    });

                    // ---- Cola: producto actualizado ----
                    cfg.ReceiveEndpoint("product-updated-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductUpdatedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.updated";
                        });
                    });

                    // ---- Cola: producto eliminado ----
                    cfg.ReceiveEndpoint("product-deleted-queue", e =>
                    {
                        e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromSeconds(5))); // 3 reintentos cada 5s
                        e.ConfigureConsumer<ProductDeletedConsumer>(context);
                        e.Bind(exchangeName, b =>
                        {
                            b.ExchangeType = ExchangeType.Direct;
                            b.RoutingKey = "product.deleted";
                        });
                    });

                    // ---- Cola de error: para procesar mensajes fallidos ----
                    cfg.ReceiveEndpoint("product-created-queue_error", e =>
                    {
                        // No usar reintentos en la cola de error para evitar loops infinitos NO vincular al exchange - MassTransit mueve mensajes automáticamente
                        e.ConfigureConsumer<ProductCreatedConsumerError>(context);
                    });
                });
            });

            var host = builder.Build();

            // Migraciones EF Core
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                db.Database.Migrate();
            }

            host.Run();
        }
    }
}