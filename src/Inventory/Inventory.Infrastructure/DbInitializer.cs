using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure
{
    public static class DbInitializer
    {
        public static async Task SeedDataAsync(InventoryDbContext context)
        {
            // Aplicar migraciones pendientes
            await context.Database.MigrateAsync();

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Alimentos", IsActive = true , CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Bebidas", IsActive = true , CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Limpieza", IsActive = true , CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Higiene personal", IsActive = true , CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Congelados", IsActive = true , CreatedAt = DateTime.UtcNow },
                    new Category { Name = "Error", IsActive = true , CreatedAt = DateTime.UtcNow },
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var categories = await context.Categories.ToListAsync();

                long alimentosId = categories.First(c => c.Name == "Alimentos").Id;
                long bebidasId = categories.First(c => c.Name == "Bebidas").Id;
                long limpiezaId = categories.First(c => c.Name == "Limpieza").Id;
                long higieneId = categories.First(c => c.Name == "Higiene personal").Id;
                long congeladosId = categories.First(c => c.Name == "Congelados").Id;

                var products = new List<Product>
                {
                    new Product
                    {
                        SKU = "ALM-001",
                        Name = "Arroz Largo Fino 1kg",
                        Description = "Arroz blanco de grano largo, ideal para guarniciones.",
                        Price = 259.90m,
                        Stock = 120,
                        Rating = 4.5,
                        ImageUrl = "https://supermercadostore.blob.core.windows.net/product-images/arroz-largo-fino.jpg",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        CategoryId = alimentosId
                    },
                    new Product
                    {
                        SKU = "BEB-002",
                        Name = "Coca-Cola 1.5L",
                        Description = "Bebida gaseosa sabor cola en botella PET de 1.5 litros.",
                        Price = 899.00m,
                        Stock = 200,
                        Rating = 4.8,
                        ImageUrl = "https://supermercadostore.blob.core.windows.net/product-images/coca-cola-1-5l.jpg",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        CategoryId = bebidasId
                    },
                    new Product
                    {
                        SKU = "LIM-003",
                        Name = "Lavandina Ayudín 1L",
                        Description = "Desinfectante multipropósito con cloro activo.",
                        Price = 320.00m,
                        Stock = 80,
                        Rating = 4.2,
                        ImageUrl = "https://supermercadostore.blob.core.windows.net/product-images/ayudin-lavandina.jpg",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        CategoryId = limpiezaId
                    },
                    new Product
                    {
                        SKU = "HIG-004",
                        Name = "Pasta Dental Colgate Triple Acción 90g",
                        Description = "Pasta dental con flúor para limpieza, protección y frescura.",
                        Price = 510.00m,
                        Stock = 95,
                        Rating = 4.6,
                        ImageUrl = "https://supermercadostore.blob.core.windows.net/product-images/colgate-triple-accion.jpg",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        CategoryId = higieneId
                    },
                    new Product
                    {
                        SKU = "CON-005",
                        Name = "Hamburguesas Congeladas Paty x4",
                        Description = "Hamburguesas de carne vacuna congeladas, pack de 4 unidades.",
                        Price = 1050.00m,
                        Stock = 50,
                        Rating = 4.4,
                        ImageUrl = "https://supermercadostore.blob.core.windows.net/product-images/paty-x4.jpg",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        CategoryId = congeladosId
                    }
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }
        }
    }
}
