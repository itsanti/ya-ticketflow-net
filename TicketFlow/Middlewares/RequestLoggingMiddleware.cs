namespace TicketFlow.Middlewares
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
            Console.WriteLine($"[TicketFlow][{DateTime.Now:HH:mm:ss}] Request: {context.Request.Method} {context.Request.Path}");

            await _next(context);

            Console.WriteLine($"[TicketFlow][{DateTime.Now:HH:mm:ss}] Response: {context.Response.StatusCode}");
        }
    }
}
