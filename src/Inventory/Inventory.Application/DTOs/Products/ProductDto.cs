using Inventory.Application.DTOs.Categories;

namespace Application.DTOs.Products
{
    public class ProductDto
    {
        public long Id { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public double Rating { get; set; }
        public string ImageUrl { get; set; }

        public CategoryDto Category { get; set; }
    }
}
