using TicketFlow.Application.DTOs.Pagination;

namespace TicketFlow.Application.DTOs.Events
{
    public class EventFiltersDto : PaginationParams
    {
        // поиск по названию (регистронезависимый, частичное совпадение)
        public string? Title { get; set; }

        // события, которые начинаются не раньше указанной даты
        public DateTime? From { get; set; }

        // события, которые заканчиваются не позже указанной даты
        public DateTime? To { get; set; }
    }
}
