using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class Product : BaseEntity
    {
        public long Id { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public double Rating { get; set; }
        public string ImageUrl { get; set; }


        public long CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
