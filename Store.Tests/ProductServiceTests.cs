using Moq;
using Store.Business.DTO;
using Store.Business.Services;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Tests
{
    [TestFixture]
    public class ProductServiceTests
    {
        private Mock<IRepository<Product>> _productRepositoryMock;
        private ProductService _productService;

        [SetUp]
        public void Setup()
        {
            this._productRepositoryMock = new Mock<IRepository<Product>>();
            this._productService = new ProductService(this._productRepositoryMock.Object);
        }

        #region CreateAsync Tests

        [TestCase("Тенісна ракетка", "Ракетка для гри в теніс", 250.0, 5)]
        [TestCase("Футбольний м'яч", "Стандартний розмір", 100.0, 20)]
        [TestCase("Гумка", "Дуже дешевий товар", 0.01, 1000)]
        [TestCase("Ексклюзивний годинник", "Товар без залишку на складі", 99999.99, 0)]
        [TestCase("Коротка назва", "", 50.0, 10)]
        public async Task CreateAsyncValidDataReturnsCreatedProductDto(string name, string description, double priceDouble, int stockQuantity)
        {
            // Arrange
            decimal price = (decimal)priceDouble;
            var createDto = new ProductCreateDto { Name = name, Description = description, Price = price, StockQuantity = stockQuantity };

            this._productRepositoryMock
                .Setup(r => r.AddAsync(It.IsAny<Product>()))
                .Callback<Product>(p => p.Id = 1);

            // Act
            var result = await this._productService.CreateAsync(createDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Description, Is.EqualTo(description));
            Assert.That(result.Price, Is.EqualTo(price));
            Assert.That(result.StockQuantity, Is.EqualTo(stockQuantity));

            this._productRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Product>()), Times.Once);
            this._productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region GetAllAsync Tests

        [Test]
        public async Task GetAllAsyncProductsExistReturnsListOfProductDtos()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Смартфон", StockQuantity = 2, Price = 1000m },
                new Product { Id = 2, Name = "Навушники", StockQuantity = 5, Price = 200m },
                new Product { Id = 3, Name = "Планшет", StockQuantity = 0, Price = 1500m }
            };
            this._productRepositoryMock.Setup(s => s.GetAllAsync()).ReturnsAsync(products);

            // Act
            var result = await this._productService.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count(), Is.EqualTo(3));
            Assert.That(result.First().Name, Is.EqualTo("Смартфон"));
            Assert.That(result.Last().Name, Is.EqualTo("Планшет"));
        }

        [Test]
        public async Task GetAllAsyncNoProductsReturnsEmptyList()
        {
            // Arrange
            this._productRepositoryMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Product>());

            // Act
            var result = await this._productService.GetAllAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        #endregion

        #region GetByIdAsync Tests

        [TestCase(1, "Смартфон", 1000.0, 10)]
        [TestCase(5, "Ноутбук", 25000.0, 2)]
        [TestCase(999, "Рідкісний товар", 0.5, 0)]
        public async Task GetByIdAsyncProductExistsReturnsProductDto(int id, string name, double priceDouble, int stock)
        {
            // Arrange
            var product = new Product { Id = id, Name = name, Price = (decimal)priceDouble, StockQuantity = stock };
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(product);

            // Act
            var result = await this._productService.GetByIdAsync(id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(id));
            Assert.That(result.Name, Is.EqualTo(name));
            Assert.That(result.Price, Is.EqualTo((decimal)priceDouble));
            Assert.That(result.StockQuantity, Is.EqualTo(stock));
        }

        [TestCase(99)]
        [TestCase(0)] // Перевірка нульового ID
        [TestCase(-1)] // Перевірка від'ємного ID
        [TestCase(-9999)]
        public async Task GetByIdAsyncProductDoesNotExistReturnsNull(int invalidId)
        {
            // Arrange
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(invalidId)).ReturnsAsync((Product?)null);

            // Act
            var result = await this._productService.GetByIdAsync(invalidId);

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region UpdateAsync Tests

        [TestCase(1, "Оновлений Товар", "Новий опис", 150.0, 10)]
        [TestCase(2, "Інший Товар", "Опис 2", 300.0, 50)]
        [TestCase(100, "Зміна ціни", "Той самий опис", 99.99, 0)]
        public async Task UpdateAsyncProductExistsUpdatesAndSaves(int id, string name, string desc, double priceDouble, int stock)
        {
            // Arrange
            var existingProduct = new Product { Id = id, Name = "Стара Назва", Price = 10m, StockQuantity = 1 };
            var updateDto = new ProductUpdateDto { Id = id, Name = name, Description = desc, Price = (decimal)priceDouble, StockQuantity = stock };

            this._productRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(existingProduct);

            // Act
            await this._productService.UpdateAsync(updateDto);

            // Assert
            Assert.That(existingProduct.Name, Is.EqualTo(name));
            Assert.That(existingProduct.Description, Is.EqualTo(desc));
            Assert.That(existingProduct.Price, Is.EqualTo((decimal)priceDouble));
            Assert.That(existingProduct.StockQuantity, Is.EqualTo(stock));

            this._productRepositoryMock.Verify(r => r.Update(existingProduct), Times.Once);
            this._productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [TestCase(99)]
        [TestCase(0)]
        [TestCase(-5)]
        public void UpdateAsyncProductDoesNotExistThrowsInvalidOperationException(int invalidId)
        {
            // Arrange
            var updateDto = new ProductUpdateDto { Id = invalidId, Name = "Тест" };
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(invalidId)).ReturnsAsync((Product?)null);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this._productService.UpdateAsync(updateDto));

            Assert.That(ex.Message, Does.Contain("не знайдено"));
            this._productRepositoryMock.Verify(r => r.Update(It.IsAny<Product>()), Times.Never);
            this._productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteAsync Tests

        [TestCase(1)]
        [TestCase(10)]
        [TestCase(999999)]
        public async Task DeleteAsyncProductExistsCallsDeleteAndSave(int id)
        {
            // Arrange
            var product = new Product { Id = id };
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(product);

            // Act
            await this._productService.DeleteAsync(id);

            // Assert
            this._productRepositoryMock.Verify(r => r.Delete(product), Times.Once);
            this._productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
        }

        [TestCase(99)]
        [TestCase(0)]
        [TestCase(-100)]
        public async Task DeleteAsyncProductDoesNotExistDoesNotCallDelete(int invalidId)
        {
            // Arrange
            this._productRepositoryMock.Setup(r => r.GetByIdAsync(invalidId)).ReturnsAsync((Product?)null);

            // Act
            await this._productService.DeleteAsync(invalidId);

            // Assert
            this._productRepositoryMock.Verify(r => r.Delete(It.IsAny<Product>()), Times.Never);
            this._productRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
        }

        #endregion
    }
}
