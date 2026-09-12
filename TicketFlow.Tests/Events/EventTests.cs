using TicketFlow.Events.Domain.Entities;
using TicketFlow.Events.Domain.Exceptions;

namespace TicketFlow.Tests.Events
{
    public class EventTests
    {
        private static Event CreateEvent(int totalSeats) => Event.Create(
            "Концерт классической музыки",
            "Вечер Бетховена в филармонии",
            new DateTime(2026, 06, 01, 19, 0, 0),
            new DateTime(2026, 06, 01, 21, 0, 0),
            totalSeats
        );

        [Fact]
        public void ChangeCapacity_ShouldKeepReservedSeats_WhenCapacityIncreased()
        {
            var eventItem = CreateEvent(10);
            Assert.True(eventItem.TryReserveSeats(4));

            eventItem.ChangeCapacity(20);

            Assert.Equal(20, eventItem.TotalSeats);
            Assert.Equal(16, eventItem.AvailableSeats);
        }

        [Fact]
        public void ChangeCapacity_ShouldKeepReservedSeats_WhenCapacityDecreased()
        {
            var eventItem = CreateEvent(10);
            Assert.True(eventItem.TryReserveSeats(4));

            eventItem.ChangeCapacity(6);

            Assert.Equal(6, eventItem.TotalSeats);
            Assert.Equal(2, eventItem.AvailableSeats);
        }

        [Fact]
        public void ChangeCapacity_ShouldAllowExactlyReservedSeats_LeavingNoFreeSeats()
        {
            var eventItem = CreateEvent(10);
            Assert.True(eventItem.TryReserveSeats(4));

            eventItem.ChangeCapacity(4);

            Assert.Equal(4, eventItem.TotalSeats);
            Assert.Equal(0, eventItem.AvailableSeats);
        }

        [Fact]
        public void ChangeCapacity_ShouldThrowValidationException_WhenLessThanReservedSeats()
        {
            var eventItem = CreateEvent(10);
            Assert.True(eventItem.TryReserveSeats(4));

            Assert.Throws<ValidationException>(() => eventItem.ChangeCapacity(3));

            Assert.Equal(10, eventItem.TotalSeats);
            Assert.Equal(6, eventItem.AvailableSeats);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ChangeCapacity_ShouldThrowValidationException_WhenNotPositive(int totalSeats)
        {
            var eventItem = CreateEvent(5);

            Assert.Throws<ValidationException>(() => eventItem.ChangeCapacity(totalSeats));
        }

        [Fact]
        public void ReleaseSeats_ShouldRestoreAvailableSeats_AfterReservation()
        {
            Event eventItem = Event.Create(
                "Концерт классической музыки",
                "Вечер Бетховена в филармонии",
                new DateTime(2026, 06, 01, 19, 0, 0),
                new DateTime(2026, 06, 01, 21, 0, 0),
                5
            );

            Assert.True(eventItem.TryReserveSeats(3));
            Assert.Equal(2, eventItem.AvailableSeats);

            eventItem.ReleaseSeats(3);

            Assert.Equal(5, eventItem.AvailableSeats);
        }
    }
}
