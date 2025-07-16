using Inventory.Application.Interfaces;
using Inventory.Application.Mapping;
using Inventory.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            //Add Automapper Configuration
            services.AddAutoMapper(typeof(MappingProfile));

            //Add Services
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICategoryService, CategoryService>();

            // Add Pending Message Services
            services.AddScoped<IPendingMessageService, PendingMessageService>();
            services.AddScoped<IResilientMessagePublisher, ResilientMessagePublisher>();

            // Add Background Service for processing pending messages
            services.AddHostedService<PendingMessageProcessorService>();

            return services;
        }
    }
}
