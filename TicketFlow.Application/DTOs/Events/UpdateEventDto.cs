using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Application.DTOs.Events
{
    public class UpdateEventDto
    {
        [Required]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public required DateTime StartAt { get; set; }

        [Required]
        public required DateTime EndAt { get; set; }

        [Required]
        public required int TotalSeats { get; set; }
    }
}
