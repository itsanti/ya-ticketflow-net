using System.ComponentModel.DataAnnotations;
using TicketFlow.Domain.Enums;

namespace TicketFlow.Application.DTOs.Users
{
    public class RegisterUserDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }

        public UserRole Role { get; set; } = UserRole.User;
    }
}
