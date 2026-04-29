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
        public ActionResult<List<Event>> GetEvents()
        {
            var events = _eventService.GetEvents();
            return Ok(events);
        }

        [HttpGet("{id}")]
        public ActionResult<Event> GetEvent(Guid id)
        {
            var eventItem = _eventService.GetEvent(id);
            if (eventItem == null)
                return NotFound();
            return Ok(eventItem);
        }

        [HttpPost]
        public ActionResult<Guid> CreateEvent(CreateEventDto dto)
        {
            try
            {
                var newEventId = _eventService.AddEvent(dto);
                return CreatedAtAction(nameof(GetEvent), new { id = newEventId }, newEventId);
            }
            catch (ArgumentException ex)
            {
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation error"
                );
            }
        }

        [HttpPut("{id}")]
        public ActionResult<Event> UpdateEvent(Guid id, UpdateEventDto dto)
        {
            try 
            {
                var updatedEvent = _eventService.UpdateEvent(id, dto);
                if (updatedEvent == null)
                    return NotFound();
                return Ok(updatedEvent);
            }
            catch (ArgumentException ex)
            {
                return Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Validation error"
                );
            }
        }

        [HttpDelete("{id}")]
        public ActionResult RemoveEvent(Guid id)
        {
            var deleted = _eventService.RemoveEvent(id);
            if (!deleted)
                return NotFound();
            return NoContent();
        }
    }
}
