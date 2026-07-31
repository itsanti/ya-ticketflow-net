using Microsoft.AspNetCore.Mvc;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Application.Services;

namespace TicketFlow.Presentation.Controllers
{
    [ApiController]
    [Route("bookings")]
    public class BookingsController : ControllerBase
    {
        private readonly IBookingService _bookingService;

        public BookingsController(IBookingService bookingService)
        {
            _bookingService = bookingService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BookingResponseDto>> GetBooking(Guid id)
        {
            return Ok(await _bookingService.GetBookingByIdAsync(id));
        }

        [HttpPost("/events/{id}/book")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid id)
        {
            var booking = await _bookingService.CreateBookingAsync(id);

            return AcceptedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
    }
}
