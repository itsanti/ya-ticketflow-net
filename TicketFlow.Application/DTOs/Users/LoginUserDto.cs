using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Application.DTOs.Users
{
    public class LoginUserDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        [StringLength(64, MinimumLength = 8)]
        public required string Password { get; set; }
    }
}
