using TicketFlow.Models;

namespace TicketFlow.Tests.Models
{
    public class BookingTests
    {
        [Fact]
        public void Confirm_ShouldSetStatusToConfirmed_AndSetProcessedAt()
        {
            var booking = new Booking(Guid.NewGuid());

            booking.Confirm();

            Assert.Equal(BookingStatus.Confirmed, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }

        [Fact]
        public void Reject_ShouldSetStatusToRejected_AndSetProcessedAt()
        {
            var booking = new Booking(Guid.NewGuid());

            booking.Reject();

            Assert.Equal(BookingStatus.Rejected, booking.Status);
            Assert.NotNull(booking.ProcessedAt);
        }
    }
}
