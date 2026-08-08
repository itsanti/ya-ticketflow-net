using Microsoft.AspNetCore.Mvc;
using System.Net;
using TicketFlow.Domain.Exceptions;

namespace TicketFlow.Presentation.Middlewares
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/json";

            var error = new ProblemDetails
            {
                Status = statusCode,
                Detail = statusCode == StatusCodes.Status500InternalServerError
                 ? "An unexpected error occurred on the server."
                 : ex.Message,
                Title = statusCode switch
                {
                    (int)HttpStatusCode.NotFound => "Not found",
                    (int)HttpStatusCode.BadRequest => "Validation error",
                    (int)HttpStatusCode.Conflict => "Conflict",
                    _ => LogAndReturn(ex)
                },
            };

            await httpContext.Response.WriteAsJsonAsync(error);
        }

        private string LogAndReturn(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return "Internal server error";
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
