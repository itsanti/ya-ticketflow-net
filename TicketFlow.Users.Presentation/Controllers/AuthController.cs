using Microsoft.AspNetCore.Mvc;
using TicketFlow.Users.Application.DTOs;
using TicketFlow.Users.Application.Services;

namespace TicketFlow.Users.Presentation.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;

        public AuthController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("register")]
        public async Task<ActionResult> Register(RegisterUserDto dto)
        {
            await _userService.RegisterAsync(dto.Login, dto.Password);
            return NoContent();
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginUserDto dto)
        {
            var token = await _userService.LoginAsync(dto.Login, dto.Password);
            return Ok(new AuthResponseDto { Token = token });
        }
    }
}
