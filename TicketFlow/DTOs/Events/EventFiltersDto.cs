namespace TicketFlow.DTOs.Events
{
    public class EventFiltersDto
    {
        // поиск по названию (регистронезависимый, частичное совпадение)
        public string? Title { get; set; }

        // события, которые начинаются не раньше указанной даты
        public DateTime? From { get; set; }

        // события, которые заканчиваются не позже указанной даты
        public DateTime? To { get; set; }
    }
}
