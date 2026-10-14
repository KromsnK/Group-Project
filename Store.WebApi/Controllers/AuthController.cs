using Microsoft.AspNetCore.Mvc;
using Store.Business.DTO;
using Store.Business.Interfaces;

namespace Store.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            this._userService = userService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            try
            {
                var result = await this._userService.RegisterAsync(dto);

                // 201 Created
                return this.CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Id }, result);
            }
            catch (InvalidOperationException ex)
            {
                return this.BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            var token = await this._userService.LoginAsync(dto.Email, dto.Password);

            if (token == null)
            {
                // 401 Unauthorized
                return this.Unauthorized(new { message = "Невірний email або пароль." });
            }

            return this.Ok(new { Token = token });
        }
    }
}
