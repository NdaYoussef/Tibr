using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tibr.Infrastructure.Exceptions;

namespace Tibr.Infrastructure.Middleware
{
    /// <summary>
    /// Global exception handling middleware to catch unhandled exceptions
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Message = "An error occurred while processing your request.",
                Success = false,
                Details = null
            };

            switch (exception)
            {
                case SeedDatabaseConnectionException seedDbEx:
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    response.Message = "Database connection failed. The service is temporarily unavailable.";
                    response.Details = seedDbEx.Message;
                    break;

                case SeedAdminCreationException seedAdminEx:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "An error occurred during initial setup.";
                    response.Details = seedAdminEx.Message;
                    break;

                case SeedDataException seedEx:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "Data seeding error occurred.";
                    response.Details = seedEx.Message;
                    break;

                case ArgumentException argEx:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    response.Message = "Invalid argument provided.";
                    response.Details = argEx.Message;
                    break;

                case UnauthorizedAccessException unAuthEx:
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    response.Message = "Unauthorized access.";
                    response.Details = unAuthEx.Message;
                    break;

                default:
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    response.Message = "An unexpected error occurred.";
                    break;
            }

            return context.Response.WriteAsJsonAsync(response);
        }

        private class ErrorResponse
        {
            public string Message { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? Details { get; set; }
        }
    }
}
