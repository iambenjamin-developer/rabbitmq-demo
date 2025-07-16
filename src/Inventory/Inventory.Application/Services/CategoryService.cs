using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Application.DTOs.Categories;
using Inventory.Application.Interfaces;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly InventoryDbContext _context;
        private readonly IMapper _mapper;

        public CategoryService(InventoryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .ProjectTo<CategoryDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }
    }
}
