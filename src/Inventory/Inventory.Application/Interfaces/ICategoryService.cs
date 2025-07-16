using Inventory.Application.DTOs.Categories;

namespace Inventory.Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
    }
}
