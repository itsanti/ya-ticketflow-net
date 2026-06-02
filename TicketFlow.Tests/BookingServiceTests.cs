using TicketFlow.Exceptions;
using TicketFlow.Models;
using TicketFlow.Models.Store;
using TicketFlow.Services;

namespace TicketFlow.Tests
{
    public class BookingServiceTests
    {
        private readonly Event eventItem = Event.Create
        (
            "Концерт классической музыки",
            "Вечер Бетховена в филармонии",
            new DateTime(2026, 06, 01, 19, 0, 0),
            new DateTime(2026, 06, 01, 21, 0, 0),
            2
        );

        [Fact]
        public async Task CreateBooking_ShouldReturnPendingBooking_WhenEventExists()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);

            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;

            await eventStore.AddAsync(eventItem);

            var booking = await service.CreateBookingAsync(eventId);

            Assert.NotEqual(Guid.Empty, booking.Id);
            Assert.Equal(eventId, booking.EventId);
            Assert.Equal(BookingStatus.Pending, booking.Status);
            Assert.True(booking.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateMultipleBookings_ShouldHaveUniqueIds()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);
            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;
            await eventStore.AddAsync(eventItem);

            var booking1 = await service.CreateBookingAsync(eventId);
            var booking2 = await service.CreateBookingAsync(eventId);

            Assert.NotEqual(booking1.Id, booking2.Id);
        }

        [Fact]
        public async Task GetBookingById_ShouldReturnCorrectBooking_WhenIdExists()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);
            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;
            await eventStore.AddAsync(eventItem);

            var createdBooking = await service.CreateBookingAsync(eventId);

            var retrievedBooking = await service.GetBookingByIdAsync(createdBooking.Id);

            Assert.NotNull(retrievedBooking);
            Assert.Equal(createdBooking.Id, retrievedBooking.Id);
            Assert.Equal(eventId, retrievedBooking.EventId);
            Assert.Equal(BookingStatus.Pending, retrievedBooking.Status);
        }

        [Fact]
        public async Task GetBookingById_ShouldThrowNotFoundException_WhenIdDoesNotExist()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.GetBookingByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetBooking_ShouldReflectStatusChange_AfterUpdateInStore()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);
            var eventId = Guid.NewGuid();
            eventItem.Id = eventId;
            await eventStore.AddAsync(eventItem);

            var booking = await service.CreateBookingAsync(eventId);

            booking.Status = BookingStatus.Confirmed;
            booking.ProcessedAt = DateTime.UtcNow;
            await store.UpdateAsync(booking);

            var updatedBooking = await service.GetBookingByIdAsync(booking.Id);

            Assert.Equal(BookingStatus.Confirmed, updatedBooking.Status);
            Assert.NotNull(updatedBooking.ProcessedAt);
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventDoesNotExist()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);
            var fakeEventId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateBookingAsync(fakeEventId));
        }

        [Fact]
        public async Task CreateBooking_ShouldThrowNotFoundException_WhenEventWasDeleted()
        {
            var store = new InMemoryBookingStore();
            var eventStore = new InMemoryEventStore();
            var service = new BookingService(store, eventStore);

            var eventItem = new Event
            {
                Id = Guid.NewGuid(),
                Title = "Тестовое событие",
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(2),
                TotalSeats = 1,
            };
            await eventStore.AddAsync(eventItem);
            await eventStore.DeleteAsync(eventItem.Id);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.CreateBookingAsync(eventItem.Id));
        }
    }
}
