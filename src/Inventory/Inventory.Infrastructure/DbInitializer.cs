using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(InventoryDbContext context)
        {
            // Migraciones pendientes
            await context.Database.MigrateAsync();

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Electrónica" },
                    new Category { Name = "Libros" },
                    new Category { Name = "Alimentos" },
                    new Category { Name = "Error simulado" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var categories = await context.Categories.ToListAsync();

                long electronicIdCategory = categories.FirstOrDefault(c => c.Name == "Electrónica")?.Id ?? 0;
                long bookIdCategory = categories.FirstOrDefault(c => c.Name == "Libros")?.Id ?? 0;
                long groceriesIdCategory = categories.FirstOrDefault(c => c.Name == "Alimentos")?.Id ?? 0;

                var products = new List<Product>
                {
                    new Product
                    {
                        Name = "Mouse Inalámbrico",
                        Description = "Mouse ergonómico inalámbrico con receptor USB",
                        Price = 29.99m,
                        Stock = 50,
                        CategoryId = electronicIdCategory
                    },
                    new Product
                    {
                        Name = "Auriculares Bluetooth",
                        Description = "Auriculares de diadema con cancelación de ruido",
                        Price = 89.99m,
                        Stock = 25,
                        CategoryId = electronicIdCategory
                    },
                    new Product
                    {
                        Name = "The Clean Coder",
                        Description = "Código de conducta para programadores profesionales",
                        Price = 39.50m,
                        Stock = 15,
                        CategoryId = bookIdCategory
                    },
                    new Product
                    {
                        Name = "Aceite de Oliva 1L",
                        Description = "Aceite de oliva virgen extra, prensado en frío",
                        Price = 10.25m,
                        Stock = 80,
                        CategoryId = groceriesIdCategory
                    },
                    new Product
                    {
                        Name = "Pasta Spaghetti 500g",
                        Description = "Pasta de trigo duro",
                        Price = 2.30m,
                        Stock = 200,
                        CategoryId = groceriesIdCategory
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
