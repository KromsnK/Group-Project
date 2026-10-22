using Moq;
using Store.Business.Services;
using Store.Data.Entities;
using Store.Data.Repositories;
using System.Linq.Expressions;
using Store.Business.DTO;
using Store.Business.Services;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Tests
{
    [TestFixture]
    public class CartServiceTests
    {
        private Mock<IRepository<Cart>> _cartRepositoryMock;
        private Mock<IRepository<CartItem>> _cartItemRepositoryMock;
        private Mock<IRepository<Product>> _productRepositoryMock;
        private CartService _cartService;

        [SetUp]
        public void Setup()
        {
            this._cartRepositoryMock = new Mock<IRepository<Cart>>();
            this._cartItemRepositoryMock = new Mock<IRepository<CartItem>>();
            this._productRepositoryMock = new Mock<IRepository<Product>>();

            this._cartService = new CartService(
                this._cartRepositoryMock.Object,
                this._cartItemRepositoryMock.Object,
                this._productRepositoryMock.Object
            );
        }

        #region GetCartByUserIdAsync Tests

        [Test]
        public async Task GetCartByUserIdAsyncCartExistsWithItemsReturnsPopulatedCartDto()
        {
            // Arrange
            int userId = 1;
            var cart = new Cart { Id = 10, UserId = userId };
            var product = new Product { Id = 100, Name = "Ноутбук", Price = 25000m };
            var cartItems = new List<CartItem>
            {
                new CartItem { Id = 1, CartId = 10, ProductId = 100, Quantity = 2 }
            };

            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart> { cart });

            this._cartItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CartItem, bool>>>()))
                .ReturnsAsync(cartItems);

            this._productRepositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Product> { product });

            // Act
            var result = await this._cartService.GetCartByUserIdAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Items.Count, Is.EqualTo(1));
            Assert.That(result.Items.First().ProductName, Is.EqualTo("Ноутбук"));
            Assert.That(result.TotalAmount, Is.EqualTo(50000m)); // 25000 * 2
        }

        [TestCase(1)]
        [TestCase(99)]
        public async Task GetCartByUserIdAsyncCartDoesNotExistReturnsEmptyCartDto(int userId)
        {
            // Arrange
            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart>());

            // Act
            var result = await this._cartService.GetCartByUserIdAsync(userId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.Items, Is.Empty);
            Assert.That(result.TotalAmount, Is.EqualTo(0m));
        }

        #endregion

        #region AddToCartAsync Tests

        [TestCase(1, 101, 2, 10)]
        [TestCase(2, 102, 1, 5)]
        public async Task AddToCartAsyncValidDataNewItemAddsToCartSuccessfully(int userId, int productId, int quantity, int stockQuantity)
        {
            // Arrange
            var product = new Product { Id = productId, Name = "Телевізор", Price = 15000m, StockQuantity = stockQuantity };
            var cart = new Cart { Id = 1, UserId = userId };

            this._productRepositoryMock.Setup(r => r.GetByIdAsync(productId)).ReturnsAsync(product);

            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart> { cart });

            this._cartItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CartItem, bool>>>()))
                .ReturnsAsync(new List<CartItem>());

            // Act
            await this._cartService.AddToCartAsync(userId, productId, quantity);

            // Assert
            this._cartItemRepositoryMock.Verify(r => r.AddAsync(It.Is<CartItem>(ci =>
                ci.CartId == cart.Id &&
                ci.ProductId == productId &&
                ci.Quantity == quantity)), Times.Once);

            this._cartItemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [TestCase(1001)]
        [TestCase(-1)]
        public void AddToCartAsyncProductNotFoundThrowsInvalidOperationException(int invalidProductId)
        {
            // Arrange
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(invalidProductId)).ReturnsAsync((Product?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this._cartService.AddToCartAsync(1, invalidProductId, 1));

            Assert.That(ex.Message, Does.Contain("Товар не знайдено"));
            this._cartItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
        }

        [TestCase(5, 10)]
        [TestCase(0, 1)]
        public void AddToCartAsyncInsufficientStockThrowsInvalidOperationException(int stock, int requestedQuantity)
        {
            // Arrange
            var product = new Product { Id = 10, StockQuantity = stock };
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this._cartService.AddToCartAsync(1, 10, requestedQuantity));

            Assert.That(ex.Message, Does.Contain("недостатньо товару"));
            this._cartItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<CartItem>()), Times.Never);
        }

        [Test]
        public void AddToCartAsyncExistingItemExceedsStockThrowsInvalidOperationException()
        {
            // Arrange
            var product = new Product { Id = 10, StockQuantity = 5 };
            var cart = new Cart { Id = 1, UserId = 1 };
            var existingItem = new CartItem { Id = 100, CartId = 1, ProductId = 10, Quantity = 4 };

            this._productRepositoryMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(product);
            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart> { cart });

            this._cartItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CartItem, bool>>>()))
                .ReturnsAsync(new List<CartItem> { existingItem });

            // Act & Assert (4 вже є + 2 нових = 6, при складі 5)
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this._cartService.AddToCartAsync(1, 10, 2));

            Assert.That(ex.Message, Does.Contain("перевищує залишок"));
        }

        #endregion

        #region RemoveFromCartAsync Tests

        [Test]
        public async Task RemoveFromCartAsyncItemExistsCallsDeleteAndSave()
        {
            // Arrange
            var cart = new Cart { Id = 1, UserId = 1 };
            var item = new CartItem { Id = 5, CartId = 1, ProductId = 10 };

            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart> { cart });

            this._cartItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CartItem, bool>>>()))
                .ReturnsAsync(new List<CartItem> { item });

            // Act
            await this._cartService.RemoveFromCartAsync(1, 10);

            // Assert
            this._cartItemRepositoryMock.Verify(r => r.Delete(item), Times.Once);
            this._cartItemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [TestCase(99, 10)]
        [TestCase(1, 999)]
        public async Task RemoveFromCartAsyncCartOrItemNotFoundDoesNotCallDelete(int userId, int productId)
        {
            // Arrange
            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart>());

            // Act
            await this._cartService.RemoveFromCartAsync(userId, productId);

            // Assert
            this._cartItemRepositoryMock.Verify(r => r.Delete(It.IsAny<CartItem>()), Times.Never);
            this._cartItemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region ClearCartAsync Tests

        [Test]
        public async Task ClearCartAsyncCartHasItemsDeletesAllItems()
        {
            // Arrange
            var cart = new Cart { Id = 1, UserId = 1 };
            var items = new List<CartItem>
            {
                new CartItem { Id = 1, CartId = 1, ProductId = 10 },
                new CartItem { Id = 2, CartId = 1, ProductId = 20 }
            };

            this._cartRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Cart, bool>>>()))
                .ReturnsAsync(new List<Cart> { cart });

            this._cartItemRepositoryMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<CartItem, bool>>>()))
                .ReturnsAsync(items);

            // Act
            await this._cartService.ClearCartAsync(1);

            // Assert
            this._cartItemRepositoryMock.Verify(r => r.Delete(It.IsAny<CartItem>()), Times.Exactly(2));
            this._cartItemRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion
    }
}
