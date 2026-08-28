using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Users.Application.DTOs
{
    public class RegisterUserDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 8)]
        public required string Password { get; set; }
    }
}
