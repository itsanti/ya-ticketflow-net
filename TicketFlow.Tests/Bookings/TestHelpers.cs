using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TicketFlow.Bookings.Application.Abstractions;
using TicketFlow.Bookings.Application.DependencyInjection;
using TicketFlow.Bookings.Domain.Entities;
using TicketFlow.Bookings.Domain.Enums;
using TicketFlow.Contracts;

namespace TicketFlow.Tests.Bookings
{
    internal sealed class TestEnvironment : IDisposable
    {
        private readonly object _sync = new();
        private readonly List<Booking> _bookings = [];

        public ServiceProvider Provider { get; }

        public Mock<IBookingRepository> BookingRepository { get; } = new();

        // Kafka-паблишер в юнит-тестах не используется — заглушка нужна только
        // чтобы GetRequiredService<IBookingConfirmedPublisher>() не падал.
        public Mock<IBookingConfirmedPublisher> BookingConfirmedPublisher { get; } = new();

        public TestEnvironment()
        {
            SetupBookingRepository();
            SetupBookingConfirmedPublisher();

            var services = new ServiceCollection();

            services.AddSingleton(BookingRepository.Object);
            services.AddSingleton(BookingConfirmedPublisher.Object);

            services.AddApplicationServices(new ConfigurationBuilder().Build());

            Provider = services.BuildServiceProvider();
        }

        public IServiceScope CreateScope() => Provider.CreateScope();

        public void Dispose() => Provider.Dispose();

        public void SeedBooking(Booking booking)
        {
            lock (_sync)
            {
                _bookings.Add(booking);
            }
        }

        public Booking? FindBooking(Guid id)
        {
            lock (_sync)
            {
                return _bookings.FirstOrDefault(b => b.Id == id);
            }
        }

        public IReadOnlyList<Booking> AllBookings()
        {
            lock (_sync)
            {
                return _bookings.ToList();
            }
        }

        private void SetupBookingRepository()
        {
            BookingRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => FindBooking(id));

            BookingRepository
                .Setup(r => r.GetByIdAsNoTrackingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => FindBooking(id));

            BookingRepository
                .Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
                .Callback((Booking booking, CancellationToken _) => SeedBooking(booking))
                .Returns(Task.CompletedTask);

            BookingRepository
                .Setup(r => r.GetPendingIdsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((CancellationToken _) =>
                {
                    lock (_sync)
                    {
                        return (IReadOnlyList<Guid>)_bookings
                            .Where(b => b.Status == BookingStatus.Pending)
                            .Select(b => b.Id)
                            .ToList();
                    }
                });

            BookingRepository
                .Setup(r => r.CountActiveBookingsByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid userId, CancellationToken _) =>
                {
                    lock (_sync)
                    {
                        return _bookings.Count(b => b.UserId == userId
                            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
                    }
                });

            BookingRepository
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private void SetupBookingConfirmedPublisher()
        {
            BookingConfirmedPublisher
                .Setup(p => p.PublishAsync(It.IsAny<BookingConfirmedEvent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }
    }

    internal static class TestHelpers
    {
        public static TestEnvironment Create() => new();
    }
}
