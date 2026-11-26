using Microsoft.AspNetCore.Diagnostics;

namespace Aurelyn.Portal.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
                var originalPath = context.Request.Path.Value;
                _logger.LogError(ex, "Unhandled exception at {Path}", originalPath);

                if (context.Response.HasStarted)
                    throw;                                  

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest"
                          || (context.Request.Headers.Accept.ToString().Contains("application/json"));
                if (isAjax)
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        status = false,
                        message = "An unexpected error occurred. Please try again."
                    });
                    return;
                }

                context.Features.Set<IExceptionHandlerPathFeature>(
                    new ExceptionHandlerFeature { Error = ex, Path = originalPath ?? "/" });

                context.SetEndpoint(null);                 
                context.Request.Path = "/Error/ServerError";
                await _next(context);
            }
        }
    }
}