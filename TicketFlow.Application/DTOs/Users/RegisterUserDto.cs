using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Application.DTOs.Users
{
    public class RegisterUserDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }
    }
}
