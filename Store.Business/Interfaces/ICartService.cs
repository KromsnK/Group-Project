using Store.Business.DTO;

namespace Store.Business.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartByUserIdAsync(int userId);
    Task AddToCartAsync(int userId, int productId, int quantity);
    Task RemoveFromCartAsync(int userId, int productId);
    Task ClearCartAsync(int userId);
}
