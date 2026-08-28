namespace TicketFlow.Bookings.Domain.Enums
{
    // Дублируется в каждом сервисе, который проверяет роль — общего проекта под enum'ы нет.
    public enum UserRole
    {
        Admin,
        User
    }
}
