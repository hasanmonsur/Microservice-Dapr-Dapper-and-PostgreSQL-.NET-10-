using Dapr;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PosBackend.Models;
using PosBackend.Repositories;
using PosBackend.Services;

namespace PosBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly PosService _posService;

        public OrdersController(PosService posService)
        {
            _posService = posService;
        }

        [HttpGet("{orderNumber}")]
        public async Task<ActionResult<Order>> GetOrder(string orderNumber)
        {
            var order = await _posService.GetOrderAsync(orderNumber);
            return order == null ? NotFound() : Ok(order);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(Order order)
        {
            var createdOrder = await _posService.CreateOrderAsync(order);

            return CreatedAtAction(nameof(GetOrder), new { orderNumber = createdOrder.OrderNumber }, createdOrder);
        }

        [HttpPatch("{orderNumber}/status")]
        public async Task<IActionResult> UpdateOrderStatus(string orderNumber, [FromBody] string status)
        {
            var success = await _posService.UpdateOrderStatusAsync(orderNumber, status);
            return success ? NoContent() : NotFound();
        }


        [Topic("pubsub", "orderCreated")]
        [HttpPost("orderCreated")]
        public async Task<IActionResult> HandleOrderCreateEvent([FromBody] Order order)
        {
            Console.WriteLine($"HandleOrderCreateEvent Handler Order Create for {order.Id}");
            
            if (order == null || order.Id<=0)
            {
                //logger.LogWarning("Invalid order received");
                return BadRequest("Invalid order data");
            }

            // 3. Save to database
              await _posService.SaveOrderAsync(order);

            return Ok();
        }

        [Topic("pubsub", "orderStatusUpdated")]
        [HttpPost("orderStatusUpdated")]
        public async Task<IActionResult> HandleOrderUpdateEvent([FromBody] Order order)
        {
            Console.WriteLine($"HandleOrderCreateEvent Handler Order Create for {order.Id}");

            if (order == null || order.Id > 0)
            {
                //logger.LogWarning("Invalid order received");
                return BadRequest("Invalid order data");
            }

            // 3. Save to database
            await _posService.UpdateOrderAsync(order);

            
            return Ok();
        }
    }
}
