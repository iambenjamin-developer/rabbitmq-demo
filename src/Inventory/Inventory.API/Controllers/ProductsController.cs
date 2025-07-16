using Application.DTOs.Products;
using Inventory.Application.DTOs.Products;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Shared.Contracts.Events;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IResilientMessagePublisher _resilientMessagePublisher;

        public ProductsController(IProductService productService, IResilientMessagePublisher resilientMessagePublisher)
        {
            _productService = productService;
            _resilientMessagePublisher = resilientMessagePublisher;
        }

        /// <summary>
        /// Obtiene la lista de todos los productos disponibles.
        /// </summary>
        /// <remarks>
        /// Devuelve todos los productos registrados en el sistema.
        /// </remarks>
        /// <response code="200">Lista de productos obtenida correctamente.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetAll()
        {
            var dtos = await _productService.GetAllAsync();
            return Ok(dtos);
        }

        /// <summary>
        /// Obtiene los detalles de un producto específico.
        /// </summary>
        /// <param name="id">ID del producto.</param>
        /// <remarks>
        /// Devuelve el producto que coincide con el ID proporcionado.
        /// </remarks>
        /// <response code="200">Producto encontrado.</response>
        /// <response code="404">Producto no encontrado.</response>
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

        /// <summary>
        /// Crea un nuevo producto.
        /// </summary>
        /// <param name="dto">Datos del producto a crear.</param>
        /// <remarks>
        /// Crea un producto en la base de datos y publica un evento <b>ProductCreated</b> mediante RabbitMQ.<br/>
        /// <b>Resiliencia:</b> Si RabbitMQ no está disponible (timeout o circuito abierto), el mensaje se guarda como pendiente en la base de datos y el endpoint responde con el código correspondiente.<br/>
        /// <ul>
        /// <li><b>201:</b> Producto creado y evento publicado exitosamente.</li>
        /// <li><b>504:</b> Timeout al publicar el evento. El mensaje se guardó como pendiente y se procesará automáticamente cuando RabbitMQ esté disponible.</li>
        /// <li><b>503:</b> Circuito abierto (RabbitMQ caído). El mensaje se guardó como pendiente y se procesará automáticamente cuando RabbitMQ esté disponible.</li>
        /// <li><b>500:</b> Error inesperado.</li>
        /// </ul>
        /// </remarks>
        /// <response code="201">Producto creado correctamente.</response>
        /// <response code="504">Tiempo de espera agotado al publicar el evento (mensaje guardado como pendiente).</response>
        /// <response code="503">Circuito abierto - RabbitMQ no disponible (mensaje guardado como pendiente).</response>
        /// <response code="500">Error inesperado.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
        {
            try
            {
                var newProductDto = await _productService.CreateAsync(dto);

                var eventMessage = new ProductCreated(
                    Id: newProductDto.Id,
                    Name: newProductDto.Name,
                    Description: newProductDto.Description,
                    Price: newProductDto.Price,
                    Stock: newProductDto.Stock,
                    Category: newProductDto.Category.Name
                );

                await _resilientMessagePublisher.PublishWithResilienceAsync(eventMessage, "product.created");

                return CreatedAtAction(nameof(GetById), new { id = newProductDto.Id }, newProductDto);
            }
            catch (TimeoutRejectedException)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Actualiza un producto existente.
        /// </summary>
        /// <param name="id">ID del producto a actualizar.</param>
        /// <param name="dto">Datos actualizados del producto.</param>
        /// <remarks>
        /// Actualiza la información del producto y publica un evento <b>ProductUpdated</b>.<br/>
        /// <b>Resiliencia:</b> Si RabbitMQ no está disponible (timeout o circuito abierto), el mensaje se guarda como pendiente y el endpoint responde con el código correspondiente.<br/>
        /// <ul>
        /// <li><b>204:</b> Actualización exitosa y evento publicado.</li>
        /// <li><b>504:</b> Timeout al publicar el evento. El mensaje se guardó como pendiente y se procesará automáticamente.</li>
        /// <li><b>503:</b> Circuito abierto. El mensaje se guardó como pendiente y se procesará automáticamente.</li>
        /// <li><b>404:</b> Producto no encontrado.</li>
        /// <li><b>500:</b> Error inesperado.</li>
        /// </ul>
        /// </remarks>
        /// <response code="204">Actualización exitosa.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="504">Timeout al publicar el evento (mensaje guardado como pendiente).</response>
        /// <response code="503">Circuito abierto (mensaje guardado como pendiente).</response>
        /// <response code="500">Error inesperado.</response>
        [HttpPut("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(long id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                bool isUpdated = await _productService.UpdateAsync(id, dto);
                if (!isUpdated)
                    return NotFound($"Producto con Id: {id} no encontrado");

                var eventMessage = new ProductUpdated(id, dto.Name, dto.Stock);

                await _resilientMessagePublisher.PublishWithResilienceAsync(eventMessage, "product.updated");

                return NoContent();
            }
            catch (TimeoutRejectedException)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina un producto por su ID.
        /// </summary>
        /// <param name="id">ID del producto a eliminar.</param>
        /// <remarks>
        /// Elimina el producto de la base de datos y publica un evento <b>ProductDeleted</b>.<br/>
        /// <b>Resiliencia:</b> Si RabbitMQ no está disponible (timeout o circuito abierto), el mensaje se guarda como pendiente y el endpoint responde con el código correspondiente.<br/>
        /// <ul>
        /// <li><b>204:</b> Eliminación exitosa y evento publicado.</li>
        /// <li><b>504:</b> Timeout al publicar el evento. El mensaje se guardó como pendiente y se procesará automáticamente.</li>
        /// <li><b>503:</b> Circuito abierto. El mensaje se guardó como pendiente y se procesará automáticamente.</li>
        /// <li><b>404:</b> Producto no encontrado.</li>
        /// <li><b>500:</b> Error inesperado.</li>
        /// </ul>
        /// </remarks>
        /// <response code="204">Eliminación exitosa.</response>
        /// <response code="404">Producto no encontrado.</response>
        /// <response code="504">Timeout al publicar el evento (mensaje guardado como pendiente).</response>
        /// <response code="503">Circuito abierto (mensaje guardado como pendiente).</response>
        /// <response code="500">Error inesperado.</response>
        [HttpDelete("{id:long}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var isDeleted = await _productService.DeleteAsync(id);
                if (!isDeleted)
                    return NotFound($"Producto con Id: {id} no encontrado");

                var eventMessage = new ProductDeleted(id);

                await _resilientMessagePublisher.PublishWithResilienceAsync(eventMessage, "product.deleted");

                return NoContent();
            }
            catch (TimeoutRejectedException)
            {
                return StatusCode(504, "⏳ Tiempo de espera agotado al publicar el evento. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (BrokenCircuitException)
            {
                return StatusCode(503, "⛔ Circuito abierto - el servicio de mensajería no está disponible. El mensaje se guardó como pendiente y se procesará cuando el servicio esté disponible.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"💥 Error inesperado: {ex.Message}");
            }
        }
    }
}
