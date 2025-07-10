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

        /// <summary>
        /// Obtiene la lista de todos los productos disponibles.
        /// </summary>
        /// <remarks>
        /// Devuelve todos los productos registrados en el sistema, ordenados por fecha de creación (más recientes primero).
        /// </remarks>
        /// <response code="200">Lista de productos obtenida correctamente.</response>
        /// <response code="500">Error inesperado en el servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            try
            {
                var result = await _productService.GetAllAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al obtener productos: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene los detalles de un producto específico por su ID.
        /// </summary>
        /// <param name="id">ID del producto a consultar.</param>
        /// <remarks>
        /// Devuelve el producto que coincide con el ID proporcionado, incluyendo información de su categoría.
        /// </remarks>
        /// <response code="200">Producto encontrado correctamente.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="500">Error inesperado en el servidor.</response>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductDto>> GetById(long id)
        {
            try
            {
                var result = await _productService.GetByIdAsync(id);
                if (result == null)
                {
                    return NotFound($"Producto con ID '{id}' no encontrado");
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al obtener el producto: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea un nuevo producto en el sistema.
        /// </summary>
        /// <param name="body">Datos del producto a crear.</param>
        /// <remarks>
        /// Crea un nuevo producto en la base de datos. El sistema valida automáticamente:
        /// - Que el SKU sea único en el sistema
        /// - Que la categoría especificada exista
        /// </remarks>
        /// <response code="201">Producto creado correctamente.</response>
        /// <response code="400">Datos inválidos o validaciones fallidas (SKU duplicado, categoría inexistente).</response>
        /// <response code="500">Error inesperado en el servidor.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto body)
        {
            try
            {
                var newProductDto = await _productService.CreateAsync(body);


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
                return BadRequest($"Error de validación: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Datos inválidos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al crear el producto: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un producto existente en el sistema.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="body">Datos actualizados del producto.</param>
        /// <remarks>
        /// Actualiza la información del producto existente. El sistema valida automáticamente:
        /// - Que el SKU sea único en el sistema (permitiendo el SKU actual del producto)
        /// - Que la categoría especificada exista
        /// - Que el ID en la ruta coincida con el ID en el cuerpo de la petición
        /// </remarks>
        /// <response code="204">Producto actualizado correctamente.</response>
        /// <response code="400">Datos inválidos o validaciones fallidas (SKU duplicado, categoría inexistente, IDs no coinciden).</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="500">Error inesperado en el servidor.</response>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto body)
        {
            try
            {
                if (id != body.Id)
                {
                    return BadRequest("El ID en la ruta debe coincidir con el ID en el cuerpo de la petición");
                }

                bool isUpdated = await _productService.UpdateAsync(id, body);
                if (!isUpdated)
                {
                    return NotFound($"Producto con ID '{id}' no encontrado");
                }


                // Publicar evento
                var eventMessage = new ProductUpdated(id, body.Name, body.Stock);
                await _publishEndpoint.Publish(eventMessage, context =>
                {
                    context.SetRoutingKey("product.updated");
                });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest($"Error de validación: {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Datos inválidos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al actualizar el producto: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un producto del sistema por su ID.
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <remarks>
        /// Elimina permanentemente el producto de la base de datos.
        /// Esta operación no se puede deshacer.
        /// </remarks>
        /// <response code="204">Producto eliminado correctamente.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="500">Error inesperado en el servidor.</response>
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
                {
                    return NotFound($"Producto con ID '{id}' no encontrado");
                }

                // Publicar evento
                await _publishEndpoint.Publish(new ProductDeleted(id), context =>
                {
                    context.SetRoutingKey("product.deleted");
                });


                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al eliminar el producto: {ex.Message}");
            }
        }
    }
}
