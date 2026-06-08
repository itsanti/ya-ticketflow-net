using TicketFlow.Models;

namespace TicketFlow.Tests.Models
{
    public class EventTests
    {
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
