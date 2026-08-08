using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Application.DTOs.Users
{
    public class RegisterUserDto
    {
        [Required]
        public required string Login { get; set; }

        [Required]
        public required string Password { get; set; }

        /// <summary>Роль пользователя: User или Admin. По умолчанию — User.</summary>
        public string Role { get; set; } = nameof(Domain.Enums.UserRole.User);
    }
}
