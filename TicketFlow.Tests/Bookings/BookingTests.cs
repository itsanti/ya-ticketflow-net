using TicketFlow.Bookings.Domain.Entities;
using TicketFlow.Bookings.Domain.Enums;

namespace TicketFlow.Tests.Bookings
{
    public class BookingTests
    {
        [Fact]
        public void Confirm_ShouldSetStatusToConfirmed_AndSetProcessedAt()
        {
            var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

            booking.Confirm();

            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        public void Reject_ShouldSetStatusToRejected_AndSetProcessedAt()
        {
            var booking = new Booking(Guid.NewGuid(), Guid.NewGuid());

            booking.Reject();

            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }
    }
}
