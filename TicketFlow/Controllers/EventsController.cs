using Microsoft.AspNetCore.Mvc;
using TicketFlow.DTOs.Events;
using TicketFlow.DTOs.Pagination;
using TicketFlow.Models;
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
        public async Task<ActionResult<PaginatedResult<Event>>> GetEvents([FromQuery] EventFiltersDto filters)
        {
            var events = await _eventService.GetEventsAsync(filters);
            return Ok(events);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var eventItem = await _eventService.GetEventAsync(id);
            return Ok(eventItem);
        }

        [HttpPost]
        public async Task<ActionResult<Guid>> CreateEvent(CreateEventDto dto)
        {
            var newEventId = await _eventService.AddEventAsync(dto);
            return CreatedAtAction(nameof(GetEvent), new { id = newEventId }, newEventId);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Event>> UpdateEvent(Guid id, UpdateEventDto dto)
        {
            var updatedEvent = await _eventService.UpdateEventAsync(id, dto);
            return Ok(updatedEvent);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> RemoveEvent(Guid id)
        {
            await _eventService.RemoveEventAsync(id);
            return NoContent();
        }
    }
}
