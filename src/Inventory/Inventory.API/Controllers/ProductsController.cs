using Application.DTOs.Products;
using Inventory.Application.DTOs.Products;
using Inventory.Application.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IPublishEndpoint _publishEndpoint;

        public ProductsController(IProductService productService, IPublishEndpoint publishEndpoint)
        {
            _productService = productService;
            _publishEndpoint = publishEndpoint;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var dtos = await _productService.GetAllAsync();
            return Ok(dtos);
        }

        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ProductDto>> GetById(long id)
        {
            var dto = await _productService.GetByIdAsync(id);
            if (dto == null)
                return NotFound($"Producto con Id: {id} no encontrado");

            return Ok(dto);
        }

        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            try
            {
                var newProductDto = await _productService.CreateAsync(dto);

                // Publicar evento
                var eventMessage = new ProductCreated(
                    Id: newProductDto.Id,
                    Name: newProductDto.Name,
                    Description: newProductDto.Description,
                    Price: newProductDto.Price,
                    Stock: newProductDto.Stock,
                    Category: newProductDto.Category.Name
                );

                await _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.created");
                });

                return CreatedAtAction(nameof(GetById), new { id = newProductDto.Id }, newProductDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado: {ex.Message}");
            }
        }

        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                bool isUpdated = await _productService.UpdateAsync(id, dto);
                if (!isUpdated)
                    return NotFound($"Producto con Id: {id} no encontrado");

                // Publicar evento
                var eventMessage = new ProductUpdated(id, dto.Name, dto.Stock);
                await _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.updated");
                });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado: {ex.Message}");
            }
        }

        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var isDeleted = await _productService.DeleteAsync(id);
                if (!isDeleted)
                    return NotFound($"Producto con Id: {id} no encontrado");

                // Publicar evento
                await _publishEndpoint.Publish(new ProductDeleted(id), context =>
                {
                    context.SetRoutingKey("product.deleted");
                });

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado: {ex.Message}");
            }
        }
    }
}
