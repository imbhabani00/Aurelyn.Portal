namespace Aurelyn.Portal.Middleware
{
    public class ProfileGateMiddleware
    {
        private readonly RequestDelegate _next;
        public ProfileGateMiddleware(RequestDelegate next) => _next = next;

        private static readonly string[] Allowed =
        {
        "/user/profile", "/user/updateprofile", "/user/ChangePassword",
        "/account/logout", "/error", "/css", "/js", "/lib", "/images", "/favicon"
        };

        public async Task InvokeAsync(HttpContext ctx)
        {
            var path = (ctx.Request.Path.Value ?? "/").ToLowerInvariant();
            var incomplete = ctx.Session.GetString("ProfileComplete") == "0";
            var isHome = path == "/" || path.StartsWith("/dashboard");  

            if (!incomplete || isHome || Allowed.Any(a => path.StartsWith(a)))
            {
                await _next(ctx);
                return;
            }

            if (ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                ctx.Response.StatusCode = 403;
                await ctx.Response.WriteAsJsonAsync(new { status = false, message = "Please complete your profile first." });
                return;
            }
            ctx.Response.Redirect("/"); 
        }
    }
}
