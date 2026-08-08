using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Application.DTOs.Users;
using TicketFlow.Application.Services;

namespace TicketFlow.Presentation.Controllers
{
    [ApiController]
    [Route("auth")]
    [AllowAnonymous]
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
            await _userService.RegisterAsync(dto.Login, dto.Password, dto.Role);
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
