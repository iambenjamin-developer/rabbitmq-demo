namespace Inventory.Domain.Entities
{
    public class Category
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public ICollection<Product> Products { get; set; }
    }
}
