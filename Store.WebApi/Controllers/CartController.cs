using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Store.Business.DTO;
using Store.Business.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Store.WebApi.Controllers
{
    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            var cart = await _cartService.GetCartByUserIdAsync(GetUserId());
            return Ok(cart);
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                await _cartService.AddToCartAsync(GetUserId(), dto.ProductId, dto.Quantity);
                return Ok(new { message = "Товар успішно додано до кошика." });
            }
            catch (InvalidOperationException ex) // Наприклад, якщо товару недостатньо на складі
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("remove/{productId}")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            await _cartService.RemoveFromCartAsync(GetUserId(), productId);
            return NoContent(); // 204 No Content
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync(GetUserId());
            return NoContent();
        }
    }
}
