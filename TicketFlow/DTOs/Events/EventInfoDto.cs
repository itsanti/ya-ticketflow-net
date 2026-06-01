using System.ComponentModel.DataAnnotations;

namespace TicketFlow.DTOs.Events
{
    public class EventInfoDto
    {
        [Required]
        public required Guid Id { get; set; }

        [Required]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public required DateTime StartAt { get; set; }

        [Required]
        public required DateTime EndAt { get; set; }

        [Required]
        public required int TotalSeats { get; set; }

        [Required]
        public required int AvailableSeats { get; set; }
    }
}
