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
            return Ok(await _eventService.GetEventAsync(id));
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
