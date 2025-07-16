using Application.DTOs.Products;
using AutoMapper;
using Inventory.Application.DTOs.Categories;
using Inventory.Application.DTOs.Products;
using Inventory.Domain.Entities;

namespace Inventory.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Categories
            CreateMap<Category, CategoryDto>();
            CreateMap<CategoryDto, Category>();

            //Products
            CreateMap<Product, ProductDto>();
            CreateMap<ProductDto, Product>();

            CreateMap<Product, CreateProductDto>();
            CreateMap<CreateProductDto, Product>();

            CreateMap<Product, UpdateProductDto>();
            CreateMap<UpdateProductDto, Product>();
        }
    }
}
