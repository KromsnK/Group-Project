using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.WebApi.Controllers;

namespace Store.Tests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<IUserService> _userServiceMock;
        private AuthController _authController;

        [SetUp]
        public void Setup()
        {
            // Service mock setup
            this._userServiceMock = new Mock<IUserService>();
            this._authController = new AuthController(this._userServiceMock.Object);
        }

        #region Register Tests

        [TestCase("test1@mail.com", "pass123", "user1", 1)]
        [TestCase("admin@store.ua", "super_secure", "admin_user", 2)]
        [TestCase("guest@shop.com", "qwerty", "guest_buyer", 99)]
        public async Task RegisterValidDataReturnsCreatedAtAction(string email, string password, string username, int expectedId)
        {
            // Arrange
            var dto = new UserRegisterDto { Email = email, Password = password, Username = username };
            var createdUser = new UserDto { Id = expectedId, Email = email, Username = username };

            _userServiceMock.Setup(s => s.RegisterAsync(It.IsAny<UserRegisterDto>())).ReturnsAsync(createdUser);

            // Act
            var result = await _authController.Register(dto);

            // Assert
            var createdResult = result as CreatedAtActionResult;
            Assert.That(createdResult, Is.Not.Null);
            Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
            Assert.That(createdResult.ActionName, Is.EqualTo("GetById"));

            var returnedUser = createdResult.Value as UserDto;
            Assert.That(returnedUser, Is.Not.Null);
            Assert.That(returnedUser!.Id, Is.EqualTo(expectedId));
            Assert.That(returnedUser.Email, Is.EqualTo(email));
        }

        [TestCase("exist@mail.com", "Користувач з таким Email вже існує.")]
        [TestCase("bad@mail.com", "Неприпустимий формат даних.")]
        public async Task RegisterServiceThrowsInvalidOperationExceptionReturnsBadRequest(string email, string errorMessage)
        {
            // Arrange
            var dto = new UserRegisterDto { Email = email, Password = "123", Username = "user1" };

            _userServiceMock.Setup(s => s.RegisterAsync(It.IsAny<UserRegisterDto>()))
                            .ThrowsAsync(new InvalidOperationException(errorMessage));

            // Act
            var result = await _authController.Register(dto);

            // Assert
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult!.StatusCode, Is.EqualTo(400));

            var responseValue = badRequestResult.Value?.ToString();
            Assert.That(responseValue, Does.Contain(errorMessage));
        }
        #endregion

        #region Login Tests

        [TestCase("user@mail.com", "MySecret123", "token_abc123")]
        [TestCase("admin@mail.com", "AdminPass!", "token_admin999")]
        public async Task LoginValidCredentialsReturnsOkWithToken(string email, string password, string expectedToken)
        {
            // Arrange
            var dto = new UserLoginDto { Email = email, Password = password };
            _userServiceMock.Setup(s => s.LoginAsync(email, password)).ReturnsAsync(expectedToken);

            // Act
            var result = await _authController.Login(dto);

            // Assert
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult!.StatusCode, Is.EqualTo(200));

            var responseValue = okResult.Value?.ToString();
            Assert.That(responseValue, Does.Contain(expectedToken)); // Перевіряємо, чи токен є у відповіді
        }

        // Перевіряємо різні варіанти невалідних даних (невірний пароль, неіснуючий email)
        [TestCase("wrong@mail.com", "12345")]
        [TestCase("user@mail.com", "wrong_password")]
        [TestCase("not_exist@mail.com", "any_password")]
        public async Task LoginInvalidCredentialsReturnsUnauthorized(string email, string password)
        {
            // Arrange
            var dto = new UserLoginDto { Email = email, Password = password };
            _userServiceMock.Setup(s => s.LoginAsync(email, password)).ReturnsAsync((string?)null);

            // Act
            var result = await _authController.Login(dto);

            // Assert
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.That(unauthorizedResult, Is.Not.Null);
            Assert.That(unauthorizedResult!.StatusCode, Is.EqualTo(401));

            var responseValue = unauthorizedResult.Value?.ToString();
            Assert.That(responseValue, Does.Contain("Невірний email або пароль"));
        }

        #endregion
    }
}
