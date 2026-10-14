using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.WebApi.Controllers;

namespace Store.Tests.Controllers
{
    [TestFixture]
    public class UsersControllerTests
    {
        private Mock<IUserService> _userServiceMock;
        private UsersController _usersController;

        [SetUp]
        public void Setup()
        {
            this._userServiceMock = new Mock<IUserService>();
            this._usersController = new UsersController(this._userServiceMock.Object);
        }

        #region GetAll Tests

        [Test]
        public async Task GetAllWhenUsersExistReturnsOkResultWithUsers()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = 1, Username = "user1" },
                new UserDto { Id = 2, Username = "user2" },
                new UserDto { Id = 3, Username = "admin" }
            };
            _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(users);

            // Act
            var result = await _usersController.GetAll();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedUsers = okResult.Value as IEnumerable<UserDto>;
            Assert.That(returnedUsers, Is.Not.Null);
            Assert.That(returnedUsers!.Count(), Is.EqualTo(3)); // Перевіряємо кількість
        }

        // ДОДАНО: Перевірка, коли користувачів у базі немає
        [Test]
        public async Task GetAllWhenNoUsersExistReturnsOkResultWithEmptyList()
        {
            // Arrange
            _userServiceMock.Setup(s => s.GetAllUsersAsync()).ReturnsAsync(new List<UserDto>());

            // Act
            var result = await _usersController.GetAll();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedUsers = okResult.Value as IEnumerable<UserDto>;
            Assert.That(returnedUsers, Is.Empty); // Перевіряємо, що повернувся пустий масив (200 OK, [] )
        }

        #endregion

        #region GetById Tests

        [TestCase(1, "user_one")]
        [TestCase(55, "gamer_dude")]
        public async Task GetByIdUserExistsReturnsOkResult(int id, string username)
        {
            // Arrange
            var user = new UserDto { Id = id, Username = username };
            _userServiceMock.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(user);

            // Act
            var result = await _usersController.GetById(id);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var returnedUser = okResult.Value as UserDto;
            Assert.That(returnedUser!.Id, Is.EqualTo(id));
            Assert.That(returnedUser.Username, Is.EqualTo(username));
        }

        // Перевіряємо різні типи невалідних ID (від'ємні, нуль, неіснуючі)
        [TestCase(0)]
        [TestCase(-5)]
        [TestCase(99999)]
        public async Task GetByIdUserDoesNotExistReturnsNotFound(int invalidId)
        {
            // Arrange
            _userServiceMock.Setup(s => s.GetByIdAsync(invalidId)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _usersController.GetById(invalidId);

            // Assert
            var notFoundResult = result as NotFoundObjectResult;
            Assert.That(notFoundResult, Is.Not.Null);
            Assert.That(notFoundResult!.StatusCode, Is.EqualTo(404));

            var responseValue = notFoundResult.Value?.ToString();
            Assert.That(responseValue, Does.Contain(invalidId.ToString())); // Перевіряємо, чи є ID у тексті помилки
        }

        #endregion

        #region Delete Tests

        // Оскільки Delete нічого не повертає (204 No Content), ми перевіряємо різні ID
        [TestCase(1)]
        [TestCase(10)]
        public async Task DeleteValidIdReturnsNoContent(int id)
        {
            // Arrange
            _userServiceMock.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

            // Act
            var result = await _usersController.Delete(id);

            // Assert
            var noContentResult = result as NoContentResult;
            Assert.That(noContentResult, Is.Not.Null);
            Assert.That(noContentResult!.StatusCode, Is.EqualTo(204));

            // Перевіряємо, що контролер дійсно викликав метод сервісу з правильним ID
            _userServiceMock.Verify(s => s.DeleteAsync(id), Times.Once);
        }

        #endregion
    }
}
