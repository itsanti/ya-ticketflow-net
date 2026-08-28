using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Bookings.Application.DTOs;
using TicketFlow.Bookings.Application.Services;
using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Bookings.Domain.Exceptions;

namespace TicketFlow.Bookings.Presentation.Controllers
{
    [ApiController]
    [Route("bookings")]
    // TODO(Этап 6): вернуть [Authorize] после подключения JWT-аутентификации в этом сервисе.
    // До этого момента GetUserId() всегда бросает UnauthorizedException — claims брать неоткуда.
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
            return Ok(await _bookingService.GetBookingByIdAsync(id, GetUserId(), GetUserRole()));
        }

        [HttpPost("/events/{id}/book")]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<BookingResponseDto>> CreateBooking(Guid id)
        {
            var booking = await _bookingService.CreateBookingAsync(id, GetUserId());

            return AcceptedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> CancelBooking(Guid id)
        {
            await _bookingService.CancelBookingAsync(id, GetUserId(), GetUserRole());

            return NoContent();
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Token does not contain a valid user identifier claim.");
            }

            return userId;
        }

        private UserRole GetUserRole()
        {
            return User.IsInRole(nameof(UserRole.Admin)) ? UserRole.Admin : UserRole.User;
        }
    }
}
