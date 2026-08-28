namespace TicketFlow.Contracts
{
    // SeatsCount сейчас всегда 1 — Booking пока не хранит количество мест.
    public sealed record BookingConfirmedEvent(
        Guid BookingId,
        Guid EventId,
        Guid UserId,
        int SeatsCount,
        DateTime ConfirmedAtUtc
    );
}
