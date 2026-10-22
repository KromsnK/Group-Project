using System.Linq.Expressions;
using Moq;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Business.Services;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        private Mock<IRepository<Order>> _orderRepositoryMock;
        private Mock<IRepository<OrderItem>> _orderItemRepositoryMock;
        private Mock<IRepository<Product>> _productRepositoryMock;
        private Mock<ICartService> _cartServiceMock;
        private OrderService _orderService;

        [SetUp]
        public void Setup()
        {
            _orderRepositoryMock = new Mock<IRepository<Order>>();
            _orderItemRepositoryMock = new Mock<IRepository<OrderItem>>();
            _productRepositoryMock = new Mock<IRepository<Product>>();
            _cartServiceMock = new Mock<ICartService>();

            _orderService = new OrderService(
                _orderRepositoryMock.Object,
                _orderItemRepositoryMock.Object,
                _productRepositoryMock.Object,
                _cartServiceMock.Object
            );
        }

        #region CreateOrderAsync Tests

        [TestCase(1, 100, 2, 10, 500.0)]
        [TestCase(2, 200, 1, 1, 1200.0)]
        public async Task CreateOrderAsyncValidCartCreatesOrderReducesStockAndClearsCart(
                    int userId, int productId, int quantity, int initialStock, double priceDouble)
        {
            // Arrange
            decimal price = (decimal)priceDouble;
            var cartDto = new CartDto
            {
                UserId = userId,
                Items = new List<CartItemDto>
                {
                    new CartItemDto
                    {
                        ProductId = productId,
                        ProductName = "Тестовий Товар",
                        Quantity = quantity,
                        Price = price
                    }
                }
            };

            var product = new Product
            {
                Id = productId,
                Name = "Тестовий Товар",
                StockQuantity = initialStock,
                Price = price
            };

            this._cartServiceMock.Setup(s => s.GetCartByUserIdAsync(userId)).ReturnsAsync(cartDto);
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

            this._orderRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Order>()))
                .Callback<Order>(o => o.Id = 777);

            // Act
            var result = await this._orderService.CreateOrderAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(777));
            Assert.That(result.TotalAmount, Is.EqualTo(quantity * price));
            Assert.That(product.StockQuantity, Is.EqualTo(initialStock - quantity));

            this._productRepositoryMock.Verify(r => r.Update(product), Times.Once);
            this._orderItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OrderItem>()), Times.Once);
            this._cartServiceMock.Verify(s => s.ClearCartAsync(userId), Times.Once);
        }
        [Test]
        public void CreateOrderAsyncEmptyCartThrowsInvalidOperationException()
        {
            // Arrange
            var emptyCart = new CartDto { UserId = 1, Items = new List<CartItemDto>() };
            _cartServiceMock.Setup(s => s.GetCartByUserIdAsync(1)).ReturnsAsync(emptyCart);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _orderService.CreateOrderAsync(1));

            Assert.That(ex.Message, Does.Contain("кошик порожній"));
            _orderRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
        }

        [Test]
        public void CreateOrderAsyncInsufficientStockOnCheckoutThrowsInvalidOperationException()
        {
            // Arrange
            var cartDto = new CartDto
            {
                UserId = 1,
                Items = new List<CartItemDto>
                {
                    new CartItemDto { ProductId = 10, ProductName = "Телефон", Quantity = 5, Price = 1000m }
                }
            };

            var product = new Product { Id = 10, StockQuantity = 2 }; // Тільки 2 шт на складі

            _cartServiceMock.Setup(s => s.GetCartByUserIdAsync(1)).ReturnsAsync(cartDto);
            _productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _orderService.CreateOrderAsync(1));

            Assert.That(ex.Message, Does.Contain("немає в достатній кількості"));
            _cartServiceMock.Verify(s => s.ClearCartAsync(It.IsAny<int>()), Times.Never);
        }

        #endregion

        #region GetUserOrdersAsync Tests

        // ПОЗИТИВНИЙ ТЕСТ: Повертає список замовлень користувача
        [Test]
        public async Task GetUserOrdersAsyncOrdersExistReturnsOrderDtoList()
        {
            // Arrange
            int userId = 1;
            var orders = new List<Order>
            {
                new Order { Id = 100, UserId = userId, TotalAmount = 500m, OrderDate = DateTime.UtcNow, OrderStatus = "Created" },
                new Order { Id = 101, UserId = userId, TotalAmount = 1500m, OrderDate = DateTime.UtcNow, OrderStatus = "Paid" }
            };

            _orderRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
                .ReturnsAsync(orders);

            // Act
            var result = await _orderService.GetUserOrdersAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetUserOrdersAsyncNoOrdersReturnsEmptyList()
        {
            // Arrange
            _orderRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Order, bool>>>()))
                .ReturnsAsync(new List<Order>());

            // Act
            var result = await _orderService.GetUserOrdersAsync(1);

            // Assert
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetOrderByIdAsync Tests

        [Test]
        public async Task GetOrderByIdAsyncOrderExistsAndBelongsToUserReturnsOrderDto()
        {
            // Arrange
            int orderId = 10;
            int userId = 1;

            var order = new Order { Id = orderId, UserId = userId, TotalAmount = 300m, OrderStatus = "Created" };
            var orderItems = new List<OrderItem> { new OrderItem { Id = 1, OrderId = orderId, ProductId = 5, Quantity = 1, UnitPrice = 300m } };
            var product = new Product { Id = 5, Name = "Мишка" };

            _orderRepositoryMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);
            _orderItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OrderItem, bool>>>()))
                .ReturnsAsync(orderItems);
            _productRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Product> { product });

            // Act
            var result = await _orderService.GetOrderByIdAsync(orderId, userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(orderId));
            Assert.That(result.Items.First().ProductName, Is.EqualTo("Мишка"));
        }

        // НЕГАТИВНИЙ/БЕЗПЕКОВИЙ ТЕСТ: Спроба відкрити чуже замовлення або неіснуюче
        [TestCase(10, 2)]
        [TestCase(99, 1)]
        public async Task GetOrderByIdAsyncOrderNotFoundOrBelongsToOtherUserReturnsNull(int requestedOrderId, int requestingUserId)
        {
            // Arrange
            var existingOrder = new Order { Id = 10, UserId = 1 };

            _orderRepositoryMock
                .Setup(r => r.GetByIdAsync(10))
                .ReturnsAsync(existingOrder);

            _orderRepositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Order?)null);

            // Act
            var result = await _orderService.GetOrderByIdAsync(requestedOrderId, requestingUserId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion
    }
}
