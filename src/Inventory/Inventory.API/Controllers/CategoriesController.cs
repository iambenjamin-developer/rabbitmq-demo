using Inventory.Application.DTOs.Categories;
using Inventory.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoriesController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Obtiene la lista de todas las categorías disponibles.
        /// </summary>
        /// <remarks>
        /// Devuelve todas las categorías registradas en el sistema.
        /// </remarks>
        /// <response code="200">Lista de categorías obtenida correctamente.</response>
        /// <response code="500">Error inesperado en el servidor.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            try
            {
                var dtos = await _categoryService.GetAllAsync();
                return Ok(dtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error inesperado al obtener categorías: {ex.Message}");
            }
        }
    }
}
