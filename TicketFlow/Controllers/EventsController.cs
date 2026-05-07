using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services;
using TicketFlow.DTOs.Events;
using TicketFlow.Models;

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
        public ActionResult<List<Event>> GetEvents([FromQuery] EventFiltersDto filters)
        {
            var events = _eventService.GetEvents(filters);
            return Ok(events);
        }

        [HttpGet("{id}")]
        public ActionResult<Event> GetEvent(Guid id)
        {
            var eventItem = _eventService.GetEvent(id);
            return Ok(eventItem);
        }

        [HttpPost]
        public ActionResult<Guid> CreateEvent(CreateEventDto dto)
        {
            var newEventId = _eventService.AddEvent(dto);
            return CreatedAtAction(nameof(GetEvent), new { id = newEventId }, newEventId);
        }

        [HttpPut("{id}")]
        public ActionResult<Event> UpdateEvent(Guid id, UpdateEventDto dto)
        {
            var updatedEvent = _eventService.UpdateEvent(id, dto);
            return Ok(updatedEvent);
        }

        [HttpDelete("{id}")]
        public ActionResult RemoveEvent(Guid id)
        {
            _eventService.RemoveEvent(id);
            return NoContent();
        }
    }
}
