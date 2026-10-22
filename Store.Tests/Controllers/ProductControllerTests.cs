using Microsoft.AspNetCore.Mvc;
using Moq;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.WebApi.Controllers;

namespace Store.Tests.Controllers
{
    [TestFixture]
    public class ProductsControllerTests
    {
        private Mock<IProductService> _productServiceMock;
        private ProductController _productsController;

        [SetUp]
        public void Setup()
        {
            this._productServiceMock = new Mock<IProductService>();
            this._productsController = new ProductController(this._productServiceMock.Object);
        }

        #region GetAll Tests

        [Test]
        public async Task GetAllWhenProductsExistReturnsOkWithProducts()
        {
            // Arrange
            var products = new List<ProductDto>
            {
                new ProductDto { Id = 1, Name = "Ноутбук", Price = 25000m, StockQuantity = 5 },
                new ProductDto { Id = 2, Name = "Мишка", Price = 500m, StockQuantity = 20 }
            };

            this._productServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(products);

            // Act
            var result = await this._productsController.GetAll();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedProducts = okResult.Value as IEnumerable<ProductDto>;
            Assert.That(returnedProducts, Is.Not.Null);
            Assert.That(returnedProducts!.Count(), Is.EqualTo(2));
        }

        [Test]
        public async Task GetAllWhenNoProductsExistReturnsOkWithEmptyList()
        {
            // Arrange
            this._productServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ProductDto>());

            // Act
            var result = await this._productsController.GetAll();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedProducts = okResult.Value as IEnumerable<ProductDto>;
            Assert.That(returnedProducts, Is.Empty);
        }

        #endregion

        #region GetById Tests

        [TestCase(1, "Ноутбук", 25000.0, 5)]
        [TestCase(10, "Клавіатура", 1500.0, 12)]
        public async Task GetByIdProductExistsReturnsOkResult(int id, string name, double priceDouble, int stock)
        {
            // Arrange
            decimal price = (decimal)priceDouble;
            var product = new ProductDto { Id = id, Name = name, Price = price, StockQuantity = stock };
            this._productServiceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(product);

            // Act
            var result = await this._productsController.GetById(id);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedProduct = okResult.Value as ProductDto;
            Assert.That(returnedProduct, Is.Not.Null);
            Assert.That(returnedProduct!.Id, Is.EqualTo(id));
            Assert.That(returnedProduct.Name, Is.EqualTo(name));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(9999)]
        public async Task GetByIdProductDoesNotExistReturnsNotFound(int invalidId)
        {
            // Arrange
            this._productServiceMock.Setup(s => s.GetByIdAsync(invalidId)).ReturnsAsync((ProductDto?)null);

            // Act
            var result = await this._productsController.GetById(invalidId);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));
        }

        #endregion

        #region Create Tests

        [TestCase("Монітор", "4K HDR", 12000.0, 3, 101)]
        [TestCase("Навушники", "Бездротові", 2500.0, 15, 102)]
        public async Task CreateValidDataReturnsCreatedAtAction(string name, string desc, double priceDouble, int stock, int createdId)
        {
            // Arrange
            decimal price = (decimal)priceDouble;
            var createDto = new ProductCreateDto { Name = name, Description = desc, Price = price, StockQuantity = stock };
            var createdDto = new ProductDto { Id = createdId, Name = name, Description = desc, Price = price, StockQuantity = stock };

            this._productServiceMock.Setup(s => s.CreateAsync(It.IsAny<ProductCreateDto>())).ReturnsAsync(createdDto);

            // Act
            var result = await this._productsController.Create(createDto);

            // Assert
            var createdResult = result as CreatedAtActionResult;
            Assert.That(createdResult, Is.Not.Null);
            Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
            Assert.That(createdResult.ActionName, Is.EqualTo(nameof(ProductController.GetById)));

            var returnedProduct = createdResult.Value as ProductDto;
            Assert.That(returnedProduct, Is.Not.Null);
            Assert.That(returnedProduct!.Id, Is.EqualTo(createdId));
        }

        #endregion

        #region Update Tests

        [TestCase(1, "Ноутбук Про", "Оновлений", 30000.0, 8)]
        [TestCase(5, "Планшет", "10 дюймів", 8000.0, 2)]
        public async Task UpdateValidDataReturnsNoContent(int id, string name, string desc, double priceDouble, int stock)
        {
            // Arrange
            decimal price = (decimal)priceDouble;
            var updateDto = new ProductUpdateDto { Id = id, Name = name, Description = desc, Price = price, StockQuantity = stock };
            this._productServiceMock.Setup(s => s.UpdateAsync(It.IsAny<ProductUpdateDto>())).Returns(Task.CompletedTask);

            // Act
            var result = await this._productsController.Update(id, updateDto);

            // Assert
            var noContentResult = result as NoContentResult;
            Assert.That(noContentResult, Is.Not.Null);
            Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));

            this._productServiceMock.Verify(s => s.UpdateAsync(It.IsAny<ProductUpdateDto>()), Times.Once);
        }

        [Test]
        public async Task UpdateIdMismatchReturnsBadRequest()
        {
            // Arrange
            int routeId = 1;
            var updateDto = new ProductUpdateDto { Id = 2, Name = "Тест", Price = 100m, StockQuantity = 1 };

            // Act
            var result = await this._productsController.Update(routeId, updateDto);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));

            this._productServiceMock.Verify(s => s.UpdateAsync(It.IsAny<ProductUpdateDto>()), Times.Never);
        }

        [Test]
        public async Task UpdateProductNotFoundInServiceReturnsBadRequest()
        {
            // Arrange
            int id = 99;
            var updateDto = new ProductUpdateDto { Id = id, Name = "Тест", Price = 100m, StockQuantity = 1 };
            this._productServiceMock.Setup(s => s.UpdateAsync(It.IsAny<ProductUpdateDto>()))
                                    .ThrowsAsync(new InvalidOperationException("Товар не знайдено"));

            // Act
            var result = await this._productsController.Update(id, updateDto);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));
        }

        #endregion

        #region Delete Tests

        [TestCase(1)]
        [TestCase(42)]
        public async Task DeleteValidIdReturnsNoContent(int id)
        {
            // Arrange
            this._productServiceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await this._productsController.Delete(id);

            // Assert
            var noContentResult = result as NoContentResult;
            Assert.That(noContentResult, Is.Not.Null);
            Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));

            this._productServiceMock.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        #endregion
    }
}
