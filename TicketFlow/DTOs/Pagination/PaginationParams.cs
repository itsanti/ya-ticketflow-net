using Microsoft.AspNetCore.Mvc;

namespace TicketFlow.DTOs.Pagination
{
    public class PaginationParams
    {
        [FromQuery(Name = "page")]
        public int Page { get; set; } = 1;

        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; } = 10;
    }
}
