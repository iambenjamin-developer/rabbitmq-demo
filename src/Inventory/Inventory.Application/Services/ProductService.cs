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

        public async Task<IEnumerable<ProductDto>> GetAllAsync()
        {
            return await _context.Products
                .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<ProductDto?> GetByIdAsync(long id)
        {
            var entity = await _context.Products
                        .Include(x => x.Category)
                        .Where(x => x.Id == id)
                        .FirstOrDefaultAsync();

            if (entity == null) return null;

            return _mapper.Map<ProductDto>(entity);
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto createDto)
        {
            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(createDto.SKU))
                throw new ArgumentException("El SKU es obligatorio.");
            if (string.IsNullOrWhiteSpace(createDto.Name))
                throw new ArgumentException("El nombre es obligatorio.");
            if (createDto.Price < 0)
                throw new ArgumentException("El precio no puede ser negativo.");
            if (createDto.Stock < 0)
                throw new ArgumentException("El stock no puede ser negativo.");

            // Validar existencia de la categoría
            bool categoryExists = await _context.Categories.AnyAsync(c => c.Id == createDto.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"La categoría con Id '{createDto.CategoryId}' no existe.");

            // Validar unicidad de SKU
            bool skuExists = await _context.Products.AnyAsync(p => p.SKU == createDto.SKU);
            if (skuExists)
                throw new InvalidOperationException($"Ya existe un producto con el SKU '{createDto.SKU}'.");

            var entity = _mapper.Map<Product>(createDto);
            _context.Products.Add(entity);
            await _context.SaveChangesAsync();

            var dto = await _context.Products
                   .Where(x => x.Id == entity.Id)
                   .ProjectTo<ProductDto>(_mapper.ConfigurationProvider)
                   .FirstOrDefaultAsync();

            return dto;
        }

        public async Task<bool> UpdateAsync(long id, UpdateProductDto updateDto)
        {
            var entity = await _context.Products.FindAsync(id);
            if (entity == null) return false;

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(updateDto.SKU))
                throw new ArgumentException("El SKU es obligatorio.");
            if (string.IsNullOrWhiteSpace(updateDto.Name))
                throw new ArgumentException("El nombre es obligatorio.");
            if (updateDto.Price < 0)
                throw new ArgumentException("El precio no puede ser negativo.");
            if (updateDto.Stock < 0)
                throw new ArgumentException("El stock no puede ser negativo.");

            // Validar existencia de la categoría
            bool categoryExists = await _context.Categories.AnyAsync(c => c.Id == updateDto.CategoryId);
            if (!categoryExists)
                throw new InvalidOperationException($"La categoría con Id '{updateDto.CategoryId}' no existe.");

            // Validar unicidad de SKU (excepto para el propio producto)
            bool skuExists = await _context.Products.AnyAsync(p => p.SKU == updateDto.SKU && p.Id != id);
            if (skuExists)
                throw new InvalidOperationException($"Ya existe un producto con el SKU '{updateDto.SKU}'.");

            _mapper.Map(updateDto, entity); // Mapea sobre la entidad existente
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
    }
}
