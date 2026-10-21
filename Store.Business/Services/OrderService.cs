using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderItem> _orderItemRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly ICartService _cartService;

        public OrderService(
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<Product> productRepository,
            ICartService cartService)
        {
            this._orderRepository = orderRepository;
            this._orderItemRepository = orderItemRepository;
            this._productRepository = productRepository;
            this._cartService = cartService;
        }

        public async Task<OrderDto> CreateOrderAsync(int userId)
        {
            var cart = await this._cartService.GetCartByUserIdAsync(userId);

            if (cart.Items == null || !cart.Items.Any())
            {
                throw new InvalidOperationException("Неможливо створити замовлення: кошик порожній.");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cart.TotalAmount,
                OrderStatus = "Created"
            };

            await this._orderRepository.AddAsync(order);
            await this._orderRepository.SaveChangesAsync();

            var orderItemsDto = new List<OrderItemDto>();

            foreach (var item in cart.Items)
            {
                var product = await this._productRepository.GetByIdAsync(item.ProductId);

                if (product == null || product.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException($"Товару '{item.ProductName}' немає в достатній кількості на складі.");
                }

                // Віднімаємо кількість зі складу
                product.StockQuantity -= item.Quantity;
                this._productRepository.Update(product);

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };

                await this._orderItemRepository.AddAsync(orderItem);

                orderItemsDto.Add(new OrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            await this._productRepository.SaveChangesAsync();
            await this._orderItemRepository.SaveChangesAsync();

            await this._cartService.ClearCartAsync(userId);

            return new OrderDto
            {
                Id = order.Id,
                UserId = userId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.OrderStatus,
                Items = orderItemsDto
            };
        }

        public async Task<IEnumerable<OrderDto>> GetUserOrdersAsync(int userId)
        {
            var orders = await _orderRepository.FindAsync(o => o.UserId == userId);

            return orders.Select(o => new OrderDto
            {
                Id = o.Id,
                UserId = o.UserId,
                OrderDate = o.OrderDate,
                TotalAmount = o.TotalAmount,
                Status = o.OrderStatus
            });
        }

        public async Task<OrderDto?> GetOrderByIdAsync(int id, int userId)
        {
            var order = await this._orderRepository.GetByIdAsync(id);

            if (order == null || order.UserId != userId)
            {
                return null;
            }

            var items = await this._orderItemRepository.FindAsync(oi => oi.OrderId == order.Id);
            var products = await this._productRepository.GetAllAsync();

            var itemsDto = items.Select(oi => {
                var product = products.FirstOrDefault(p => p.Id == oi.ProductId);
                return new OrderItemDto
                {
                    ProductId = oi.ProductId,
                    ProductName = product?.Name ?? "Невідомий товар",
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                };
            }).ToList();

            return new OrderDto
            {
                Id = order.Id,
                UserId = order.UserId,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.OrderStatus,
                Items = itemsDto
            };
        }
    }
}
