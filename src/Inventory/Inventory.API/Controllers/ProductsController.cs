using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Shared.Contracts.Events;

namespace Inventory.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IPublishEndpoint _publishEndpoint;

        public ProductsController(IPublishEndpoint publishEndpoint)
        {
            _publishEndpoint = publishEndpoint;
        }


        [HttpPost]
        public async Task<IActionResult> Create([FromBody] bool throwError = false)
        {
            var category = throwError ? "Error simulado" : "Libros";
            var eventMessage = new ProductCreated
                (
                Id: 7,
                Name: "El Arte de la Guerra",
                Description: "Tratado estratégico con aplicaciones modernas en negocios",
                Price: 12.75m,
                Stock: 8,
                Category: category
                );

            await _publishEndpoint.Publish(eventMessage, context =>
            {
                context.SetRoutingKey("product.created");
            });

            return Ok();
        }


        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id)
        {
            var eventMessage = new ProductUpdated(id, "Sapiens: De animales a dioses", 10);

            await _publishEndpoint.Publish(eventMessage, context =>
            {
                context.SetRoutingKey("product.updated");
            });

            return Ok();
        }


        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            await _publishEndpoint.Publish(new ProductDeleted(id), context =>
            {
                context.SetRoutingKey("product.deleted");
            });

            return Ok();
        }
    }
}
