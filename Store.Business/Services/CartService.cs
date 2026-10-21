using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Business.Services
{
    public class CartService : ICartService
    {
        private readonly IRepository<Cart> _cartRepository;
        private readonly IRepository<CartItem> _cartItemRepository;
        private readonly IRepository<Product> _productRepository;

        public CartService(
            IRepository<Cart> cartRepository,
            IRepository<CartItem> cartItemRepository,
            IRepository<Product> productRepository)
        {
            this._cartRepository = cartRepository;
            this._cartItemRepository = cartItemRepository;
            this._productRepository = productRepository;
        }

        public async Task<CartDto> GetCartByUserIdAsync(int userId)
        {
            var carts = await this._cartRepository.FindAsync(c => c.UserId == userId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
            {
                return new CartDto { UserId = userId };
            }

            var cartItems = await this._cartItemRepository.FindAsync(ci => ci.CartId == cart.Id);
            var products = await this._productRepository.GetAllAsync();

            var itemsDto = cartItems.Select(ci => {
                var product = products.First(p => p.Id == ci.ProductId);
                return new CartItemDto
                {
                    Id = ci.Id,
                    ProductId = ci.ProductId,
                    ProductName = product.Name,
                    Quantity = ci.Quantity,
                    Price = product.Price
                };
            }).ToList();

            return new CartDto
            {
                UserId = userId,
                Items = itemsDto
            };
        }

        public async Task AddToCartAsync(int userId, int productId, int quantity)
        {
            var product = await this._productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new InvalidOperationException("Товар не знайдено.");
            }

            if (product.StockQuantity < quantity)
            {
                throw new InvalidOperationException($"На складі недостатньо товару. Доступно: {product.StockQuantity}");
            }

            var carts = await this._cartRepository.FindAsync(c => c.UserId == userId);
            var cart = carts.FirstOrDefault();

            if (cart == null)
            {
                cart = new Cart { UserId = userId };
                await this._cartRepository.AddAsync(cart);
                await this._cartRepository.SaveChangesAsync();
            }

            var existingItems = await this._cartItemRepository.FindAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            var existingItem = existingItems.FirstOrDefault();

            if (existingItem != null)
            {
                if (product.StockQuantity < existingItem.Quantity + quantity)
                {
                    throw new InvalidOperationException("Загальна кількість у кошику перевищує залишок на складі.");
                }

                existingItem.Quantity += quantity;
                this._cartItemRepository.Update(existingItem);
            }
            else
            {
                var newItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity
                };
                await this._cartItemRepository.AddAsync(newItem);
            }

            await this._cartItemRepository.SaveChangesAsync();
        }

        public async Task RemoveFromCartAsync(int userId, int productId)
        {
            var carts = await this._cartRepository.FindAsync(c => c.UserId == userId);
            var cart = carts.FirstOrDefault();
            if (cart == null)
            {
                return;
            }

            var items = await this._cartItemRepository.FindAsync(ci => ci.CartId == cart.Id && ci.ProductId == productId);
            var item = items.FirstOrDefault();

            if (item != null)
            {
                this._cartItemRepository.Delete(item);
                await this._cartItemRepository.SaveChangesAsync();
            }
        }

        public async Task ClearCartAsync(int userId)
        {
            var carts = await this._cartRepository.FindAsync(c => c.UserId == userId);
            var cart = carts.FirstOrDefault();
            if (cart == null)
            {
                return;
            }

            var items = await this._cartItemRepository.FindAsync(ci => ci.CartId == cart.Id);
            foreach (var item in items)
            {
                this._cartItemRepository.Delete(item);
            }
            await this._cartItemRepository.SaveChangesAsync();
        }
    }
}
