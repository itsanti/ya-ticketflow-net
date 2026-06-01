using Microsoft.AspNetCore.Mvc;
using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Services;

namespace TicketFlow.Controllers
{
    [ApiController]
    [Route("events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;

        public EventsController(IEventService eventService)
        {
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<ActionResult<PaginatedResult<EventInfoDto>>> GetEvents([FromQuery] EventFiltersDto filters)
        {
            return Ok(await _eventService.GetEventsAsync(filters));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EventInfoDto>> GetEvent(Guid id)
        {
            var event_ = await _eventService.GetEventAsync(id);
            var result = new EventInfoDto
            {
                Id = event_.Id,
                Title = event_.Title,
                Description = event_.Description,
                StartAt = event_.StartAt,
                EndAt = event_.EndAt,
                TotalSeats = event_.TotalSeats,
                AvailableSeats = event_.AvailableSeats
            };
            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateEvent(CreateEventDto dto)
        {
            var newEventId = await _eventService.AddEventAsync(dto);
            return CreatedAtAction(nameof(GetEvent), new { id = newEventId }, newEventId);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<EventInfoDto>> UpdateEvent(Guid id, UpdateEventDto dto)
        {
            return Ok(await _eventService.UpdateEventAsync(id, dto));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveEvent(Guid id)
        {
            await _eventService.RemoveEventAsync(id);
            return NoContent();
        }
    }
}
