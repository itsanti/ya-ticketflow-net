using Microsoft.AspNetCore.Mvc;
using TicketFlow.Services;

namespace TicketFlow.Controllers
{
    [ApiController]
    [Route("bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IEventService _eventService;

        public BookingsController(IBookingService bookingService, IEventService eventService)
        {
            _bookingService = bookingService;
            _eventService = eventService;
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            return Ok(booking);
        }

        [HttpPost("/events/{id}/book")]
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            _eventService.GetEvent(id);

            var booking = await _bookingService.CreateBookingAsync(id);

            return AcceptedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
    }
}
