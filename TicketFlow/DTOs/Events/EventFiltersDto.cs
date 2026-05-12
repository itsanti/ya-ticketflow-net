using Microsoft.AspNetCore.Mvc;
using TicketFlow.DTOs.Pagination;

namespace TicketFlow.DTOs.Events
{
    public class EventFiltersDto : PaginationParams
    {
        // поиск по названию (регистронезависимый, частичное совпадение)
        [FromQuery(Name = "title")]
        public string? Title { get; set; }

        // события, которые начинаются не раньше указанной даты
        [FromQuery(Name = "from")]
        public DateTime? From { get; set; }

        // события, которые заканчиваются не позже указанной даты
        [FromQuery(Name = "to")]
        public DateTime? To { get; set; }
    }
}
