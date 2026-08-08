using Microsoft.AspNetCore.Mvc;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Presentation.Middlewares
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger,
            IProblemDetailsService problemDetailsService)
        {
            _next = next;
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                await HandleException(httpContext, ex);
            }
        }

        private async Task HandleException(HttpContext httpContext, Exception ex)
        {
            if (httpContext.Response.HasStarted)
            {
                return;
            }

            var statusCode = MapStatusCode(ex);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(ex, ex.Message);
            }

            httpContext.Response.StatusCode = statusCode;

            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Detail = statusCode == StatusCodes.Status500InternalServerError
                        ? "An unexpected error occurred on the server."
                        : ex.Message
                }
            });
        }

        private static int MapStatusCode(Exception ex)
            => ex switch
            {
                ValidationException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                NoAvailableSeatsException => StatusCodes.Status409Conflict,
                EventAlreadyStartedException => StatusCodes.Status400BadRequest,
                BookingLimitExceededException => StatusCodes.Status409Conflict,
                ForbiddenException => StatusCodes.Status403Forbidden,
                InvalidOperationDomainException => StatusCodes.Status400BadRequest,
                DomainException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };
    }
}
