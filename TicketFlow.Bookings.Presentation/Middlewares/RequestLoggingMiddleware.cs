namespace TicketFlow.Bookings.Presentation.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            Console.WriteLine($"[TicketFlow.Bookings][{DateTime.Now:HH:mm:ss}] Request: {context.Request.Method} {context.Request.Path}");

            await _next(context);

            Console.WriteLine($"[TicketFlow.Bookings][{DateTime.Now:HH:mm:ss}] Response: {context.Response.StatusCode}");
        }
    }
}
