using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketFlow.Application.DTOs.Bookings;
using TicketFlow.Application.Services;
using TicketFlow.Domain.Enums;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Presentation.Controllers
{
    [ApiController]
    [Route("bookings")]
    [Authorize]
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
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

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
