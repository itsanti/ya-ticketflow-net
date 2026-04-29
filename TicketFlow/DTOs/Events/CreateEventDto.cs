using System.ComponentModel.DataAnnotations;

namespace TicketFlow.DTOs.Events
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
    }
}
