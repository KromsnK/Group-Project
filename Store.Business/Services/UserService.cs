using Microsoft.Extensions.Logging;
using Store.Business.DTO;
using Store.Business.Interfaces;
using Store.Data.Entities;
using Store.Data.Repositories;

namespace Store.Business.Services;

public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IRepository<User> userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        _logger.LogInformation("Отримання користувача з ID: {UserId}", id);
        var user = await _userRepository.GetByIdAsync(id);

        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        _logger.LogInformation("Отримання списку всіх користувачів");
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<UserDto> RegisterAsync(UserRegisterDto dto)
    {
        _logger.LogInformation("Спроба реєстрації користувача з Email: {Email}", dto.Email);

        var existingUsers = await _userRepository.FindAsync(u => u.Email == dto.Email);
        if (existingUsers.Any())
        {
            _logger.LogWarning("Реєстрація відхилена: Email {Email} вже зайнятий", dto.Email);
            throw new InvalidOperationException("Користувач з таким Email вже існує.");
        }

        var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(dto.Password));

        var newUser = new User
        {
            Email = dto.Email,
            PasswordHash = passwordHash,
            Username = dto.Username,
            Role = "Customer" 
        };

        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        _logger.LogInformation("Користувача {Email} успішно створено з ID {UserId}", newUser.Email, newUser.Id);

        return MapToDto(newUser);
    }

    public async Task<string?> LoginAsync(string email, string password)
    {
        _logger.LogInformation("Спроба входу для: {Email}", email);

        var users = await _userRepository.FindAsync(u => u.Email == email);
        var user = users.FirstOrDefault();

        if (user == null) return null;

        var incomingHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));
        if (user.PasswordHash != incomingHash)
        {
            _logger.LogWarning("Невірний пароль для користувача: {Email}", email);
            return null;
        }

        return "mock-jwt-token";
    }

    public async Task DeleteAsync(int id)
    {
        _logger.LogInformation("Видалення користувача з ID: {UserId}", id);
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            _userRepository.Delete(user);
            await _userRepository.SaveChangesAsync();
        }
    }

    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Username = user.Username,
            Role = user.Role
        };
    }
}
