using Microsoft.AspNetCore.Mvc;
using Store.Business.Interfaces;

namespace Store.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            this._userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await this._userService.GetAllUsersAsync();
            return this.Ok(users); // 200 OK
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await this._userService.GetByIdAsync(id);

            if (user == null)
            {
                return this.NotFound(new { message = $"Користувача з ID {id} не знайдено." }); // 404 Not Found
            }

            return this.Ok(user); // 200 OK
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await this._userService.DeleteAsync(id);
            return this.NoContent(); // 204 No Content
        }
    }
}
