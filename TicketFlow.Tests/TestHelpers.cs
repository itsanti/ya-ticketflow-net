using TicketFlow.Models;

namespace TicketFlow.Tests
{
    internal static class TestHelpers
    {
        internal static Event CreateTestEvent(int totalSeats)
        {
            return Event.Create(
                "Тестовое событие",
                "Описание тестового события",
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(1).AddHours(2),
                totalSeats
            );
        }
    }
}
