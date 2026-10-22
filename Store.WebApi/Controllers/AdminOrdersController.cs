using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Business.Interfaces;
using System.Threading.Tasks;

namespace Store.WebApi.Controllers
{
    [Authorize(Roles = "Admin")] 
    [ApiController]
    [Route("api/admin/orders")]
    public class AdminOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public AdminOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public class UpdateStatusDto
        {
            public string Status { get; set; } = string.Empty;
        }


        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeOrderStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var updatedOrder = await _orderService.UpdateOrderStatusAsync(id, dto.Status);
                return Ok(updatedOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}