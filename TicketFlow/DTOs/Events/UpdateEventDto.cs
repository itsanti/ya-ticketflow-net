namespace TicketFlow.DTOs.Events
{
    public class UpdateEventDto
    {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public required DateTime StartAt { get; set; }
        public required DateTime EndAt { get; set; }
    }
}
