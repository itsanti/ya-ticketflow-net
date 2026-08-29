using System.ComponentModel.DataAnnotations;

namespace TicketFlow.Events.Application.DTOs
{
    public class CreateEventDto
    {
        [Required]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public required DateTime StartAt { get; set; }

        [Required]
        public required DateTime EndAt { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public required int TotalSeats { get; set; }
    }
}
