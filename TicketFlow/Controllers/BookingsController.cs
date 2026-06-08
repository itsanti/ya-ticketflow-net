using Microsoft.AspNetCore.Mvc;
using TicketFlow.DTOs.Bookings;
using TicketFlow.Services;

namespace TicketFlow.Controllers
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
        public async Task<IActionResult> GetBooking(Guid id)
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);

            var responseDto = new BookingResponseDto
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };

            return Ok(responseDto);
        }

        [HttpPost("/events/{id}/book")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateBooking(Guid id)
        {
            var booking = await _bookingService.CreateBookingAsync(id);

            var responseDto = new BookingResponseDto
            {
                Id = booking.Id,
                EventId = booking.EventId,
                Status = booking.Status.ToString(),
                CreatedAt = booking.CreatedAt,
                ProcessedAt = booking.ProcessedAt
            };

            return AcceptedAtAction(nameof(GetBooking), new { id = responseDto.Id }, responseDto);
        }
    }
}
