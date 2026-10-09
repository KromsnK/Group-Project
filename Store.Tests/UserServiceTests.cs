using System.Linq.Expressions;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Store.Business.DTO;
using Store.Business.Services;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Tests;

[TestFixture]
public class UserServiceTests
{
    private Mock<IRepository<User>> _userRepositoryMock;
    private Mock<ILogger<UserService>> _loggerMock;
    private UserService _userService;

    [SetUp]
    public void Setup()
    {
        this._userRepositoryMock = new Mock<IRepository<User>>();
        this._loggerMock = new Mock<ILogger<UserService>>();
        this._userService = new UserService(this._userRepositoryMock.Object, this._loggerMock.Object);
    }

    #region GetByIdAsync Tests

    [Test]
    public async Task GetByIdAsyncUserExistsReturnsUserDto()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@test.com", Username = "ivan_user", Role = "Customer" };
        this._userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        var result = await this._userService.GetByIdAsync(1);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Email, Is.EqualTo("test@test.com"));
        Assert.That(result.Username, Is.EqualTo("ivan_user"));
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(999)]
    public async Task GetByIdAsyncUserDoesNotExistReturnsNull(int invalidId)
    {
        // Arrange
        this._userRepositoryMock.Setup(r => r.GetByIdAsync(invalidId)).ReturnsAsync((User?)null);

        // Act
        var result = await this._userService.GetByIdAsync(invalidId);

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region GetAllUsersAsync Tests

    [Test]
    public async Task GetAllUsersAsyncUsersExistReturnsDtoList()
    {
        // Arrange
        var users = new List<User>
            {
                new User { Id = 1, Email = "u1@test.com", Username = "user_one" },
                new User { Id = 2, Email = "u2@test.com", Username = "user_two" }
            };
        this._userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

        // Act
        var result = await this._userService.GetAllUsersAsync();

        // Assert
        Assert.That(result.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task GetAllUsersAsyncNoUsersReturnsEmptyList()
    {
        // Arrange
        this._userRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<User>());

        // Act
        var result = await this._userService.GetAllUsersAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region RegisterAsync Tests

    [TestCase("new@mail.com", "pass123", "anna_smith")]
    [TestCase("admin@store.ua", "super_secure", "john_doe")]
    public async Task RegisterAsyncValidDataReturnsCreatedDto(string email, string password, string username)
    {
        // Arrange
        var dto = new UserRegisterDto { Email = email, Password = password, Username = username };

        this._userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                           .ReturnsAsync(new List<User>());

        // Act
        var result = await this._userService.RegisterAsync(dto);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Email, Is.EqualTo(email));
        Assert.That(result.Username, Is.EqualTo(username)); // Перевіряємо Username

        this._userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        this._userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public void RegisterAsyncEmailAlreadyExistsThrowsInvalidOperationException()
    {
        // Arrange
        var dto = new UserRegisterDto { Email = "exist@mail.com", Password = "123", Username = "test_user" };
        var existingUserList = new List<User> { new User { Email = "exist@mail.com", Username = "old_user" } };

        this._userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                           .ReturnsAsync(existingUserList);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => await this._userService.RegisterAsync(dto));
        Assert.That(ex.Message, Does.Contain("вже існує"));

        this._userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        this._userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion

    #region LoginAsync Tests

    [TestCase("user@mail.com", "MySecret123")]
    [TestCase("admin@mail.com", "AdminPass!")]
    public async Task LoginAsyncValidCredentialsReturnsToken(string email, string password)
    {
        // Arrange
        var expectedHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
        var user = new User { Email = email, PasswordHash = expectedHash, Username = "auth_user" };

        this._userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                           .ReturnsAsync(new List<User> { user });

        // Act
        var result = await this._userService.LoginAsync(email, password);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo("mock-jwt-token"));
    }

    [TestCase("user@mail.com", "WrongPass")]
    [TestCase("user@mail.com", "")]
    public async Task LoginAsyncInvalidPasswordReturnsNull(string email, string wrongPassword)
    {
        // Arrange
        var correctHash = Convert.ToBase64String(Encoding.UTF8.GetBytes("CorrectPass"));
        var user = new User { Email = email, PasswordHash = correctHash, Username = "auth_user" };

        this._userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                           .ReturnsAsync(new List<User> { user });

        // Act
        var result = await this._userService.LoginAsync(email, wrongPassword);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task LoginAsyncUserNotFoundReturnsNull()
    {
        // Arrange
        this._userRepositoryMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                           .ReturnsAsync(new List<User>());

        // Act
        var result = await this._userService.LoginAsync("notfound@mail.com", "anypass");

        // Assert
        Assert.That(result, Is.Null);
    }

    #endregion

    #region DeleteAsync Tests

    [Test]
    public async Task DeleteAsyncUserExistsCallsDeleteAndSave()
    {
        // Arrange
        var user = new User { Id = 1, Username = "to_delete" };
        this._userRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        // Act
        await this._userService.DeleteAsync(1);

        // Assert
        this._userRepositoryMock.Verify(r => r.Delete(user), Times.Once);
        this._userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [TestCase(99)]
    [TestCase(0)]
    public async Task DeleteAsyncUserDoesNotExistDoesNotCallDelete(int invalidId)
    {
        // Arrange
        this._userRepositoryMock.Setup(r => r.GetByIdAsync(invalidId)).ReturnsAsync((User?)null);

        // Act
        await this._userService.DeleteAsync(invalidId);

        // Assert
        this._userRepositoryMock.Verify(r => r.Delete(It.IsAny<User>()), Times.Never);
        this._userRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    #endregion
}
