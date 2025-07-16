using Application.DTOs.Products;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Inventory.Application.DTOs.Products;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly InventoryDbContext _context;
        private readonly IMapper _mapper;

        public ProductService(InventoryDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        #region Public Queries

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            var result = await _context.Products
                        .AsNoTracking()
                        .OrderByDescending(p => p.CreatedAt)
                        .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                        .ToListAsync();

            return result;
        }

        public async Task<ProductDto> GetByIdAsync(long id)
        {
            var entity = await _context.Products
                        .AsNoTracking()
                        .Include(p => p.Category)
                        .Where(p => p.Id == id)
                        .FirstOrDefaultAsync();

            var dto = _mapper.Map<ProductDto>(entity);

            return dto;
        }

        #endregion

        #region Public Commands
        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            await EnsureCategoryExistsAsync(createDto.CategoryId);
            await EnsureUniqueSkuAsync(createDto.SKU);

            var entity = _mapper.Map<Product>(createDto);
            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            var result = await GetByIdAsync(entity.Id);

            return result;
        }

        public async Task<bool> UpdateAsync(long id, UpdateProductDto updateDto)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null) return false;
            await EnsureCategoryExistsAsync(updateDto.CategoryId);
            await EnsureUniqueSkuAsync(updateDto.SKU);

            _mapper.Map(updateDto, entity); // Mapea todos los campos a actualizar de entity
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(long id)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null) return false;

            _context.Products.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Private Helpers

        private async Task EnsureCategoryExistsAsync(long categoryId)
        {
            var exists = await _context.Categories.AnyAsync(c => c.Id == categoryId);
            if (!exists)
            {
                throw new InvalidOperationException($"La categoría con Id '{categoryId}' no existe.");
            }
        }

        private async Task EnsureUniqueSkuAsync(string sku, long? currentProductId = null)
        {
            var exists = await _context.Products.AnyAsync(p => p.SKU == sku && (!currentProductId.HasValue || p.Id != currentProductId));
            if (exists)
            {
                throw new InvalidOperationException($"Ya existe un producto con el SKU '{sku}'.");
            }
        }

        #endregion
    }
}
