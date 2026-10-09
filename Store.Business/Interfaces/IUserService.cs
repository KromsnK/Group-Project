using System.Threading.Tasks;
using Store.Business.DTO;

namespace Store.Business.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<UserDto> RegisterAsync(UserRegisterDto dto);
    Task<string?> LoginAsync(string email, string password);
    Task DeleteAsync(int id);
}
