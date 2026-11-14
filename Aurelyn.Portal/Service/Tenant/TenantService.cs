namespace Aurelyn.Portal.Service.Tenant
{
    #region Interface
    public interface ITenantService
    {
        string? GetTenantCode();
    }
    #endregion

    public class TenantService : ITenantService
    {
        #region Properties
        private readonly IHttpContextAccessor _http;
        private readonly IConfiguration _config;
        #endregion


        #region Constructor
        public TenantService(IHttpContextAccessor http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }
        #endregion

        #region GetTenantCode
        public string? GetTenantCode()
        {
            var ctx = _http.HttpContext;

            if (ctx != null)
            {
                // 1. Route: /t/{tenant}/account/register
                if (ctx.Request.RouteValues.TryGetValue("tenant", out var routeTenant)
                    && routeTenant is string rt && !string.IsNullOrWhiteSpace(rt))
                    return rt.Trim().ToUpperInvariant();

                var parts = ctx.Request.Host.Host.Split('.');

                // 2a. apollo.localhost
                if (parts.Length == 2 && parts[1].Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    return parts[0].ToUpperInvariant();

                // 2b. apollo.aurelyn.com
                if (parts.Length >= 3 && !parts[0].Equals("www", StringComparison.OrdinalIgnoreCase))
                    return parts[0].ToUpperInvariant();
            }

            // 3. Fallback: appsettings.json
            return _config["Tenant:DefaultCode"];
        }
        #endregion
    }
}