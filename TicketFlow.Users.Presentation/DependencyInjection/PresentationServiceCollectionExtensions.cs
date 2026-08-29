using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace TicketFlow.Users.Presentation.DependencyInjection
{
    public static class PresentationServiceCollectionExtensions
    {
        public static IServiceCollection AddPresentationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var errors = context.ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage);
                        var detailMessage = string.Join(" ", errors);
                        var problemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status400BadRequest,
                            Title = "Validation error",
                            Detail = detailMessage
                        };

                        return new BadRequestObjectResult(problemDetails);
                    };
                });

            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Type = null;
                    context.ProblemDetails.Title = context.ProblemDetails.Status switch
                    {
                        StatusCodes.Status400BadRequest => "Validation error",
                        StatusCodes.Status401Unauthorized => "Unauthorized",
                        StatusCodes.Status404NotFound => "Not found",
                        StatusCodes.Status500InternalServerError => "Internal server error",
                        _ => context.ProblemDetails.Title
                    };
                };
            });

            // Users не проверяет JWT (у него нет защищённых эндпоинтов — register/login анонимны),
            // поэтому AddAuthentication/AddJwtBearer здесь не подключаем.
            services.AddOpenApi();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
