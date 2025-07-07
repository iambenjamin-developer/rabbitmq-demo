using Inventory.Domain.Common;

namespace Inventory.Domain.Entities
{
    public class Category : BaseEntity
    {
        public long Id { get; set; }
        public string Name { get; set; }


        public ICollection<Product> Products { get; set; }
    }
}
