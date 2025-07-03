using MassTransit;
using RabbitMQ.Client;
using Shared.Contracts.Events;

namespace Inventory.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            // MassTransit + RabbitMQ
            builder.Services.AddMassTransit(x =>
            {
                x.SetKebabCaseEndpointNameFormatter();

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

                    const string exchangeName = "inventory_exchange";

                    // Asignar exchange comun
                    cfg.Message<ProductCreated>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductCreated>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    cfg.Message<ProductUpdated>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductUpdated>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    cfg.Message<ProductDeleted>(m => m.SetEntityName(exchangeName));
                    cfg.Publish<ProductDeleted>(p =>
                    {
                        p.ExchangeType = ExchangeType.Direct;
                    });

                    // NO usar ConfigureEndpoints ya que esta API no consume mensajes
                });
            });

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
