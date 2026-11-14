using Microsoft.AspNetCore.Diagnostics;

namespace Aurelyn.Portal.Middleware
{
    public class ExceptionMiddleware
    {
        #region Properties
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        #endregion

        #region Constructor
        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        #region Invoke
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();

                context.Response.StatusCode =
                    StatusCodes.Status500InternalServerError;

                context.Request.Path = "/Error/ServerError";

                var exceptionHandler =
                    context.RequestServices.GetRequiredService<
                        IExceptionHandlerPathFeature>();

                await context.Response.WriteAsync(
                    "An unexpected error occurred.");
            }
        }
        #endregion
    }
}