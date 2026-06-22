using Microsoft.AspNetCore.Builder;
using Tibr.Infrastructure.Middleware;

namespace Tibr.Infrastructure.Extensions
{
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Adds global exception handling middleware to the application
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }
    }
}
