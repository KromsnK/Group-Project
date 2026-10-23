using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Store.Business.DTO;
using Store.Business.Interfaces;

namespace Store.WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpPost("checkout")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var order = await _orderService.CreateOrderAsync(GetUserId());

                return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await _orderService.GetUserOrdersAsync(GetUserId());
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id, GetUserId());

            if (order == null)
            {
                return NotFound(new { message = "Замовлення не знайдено або у вас немає доступу до нього." });
            }

            return Ok(order);
        }
    }
}
