using Aurelyn.Portal.Configurations;
using Aurelyn.Portal.Constants;
using Aurelyn.Portal.Models.Account;
using Aurelyn.Portal.Models.Common;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Aurelyn.Portal.Middleware
{
    public class AuthMiddleware
    {
        #region Properties
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthMiddleware> _logger;
        #endregion

        #region Constructor
        public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        #endregion

        #region Invoke
        public async Task InvokeAsync(HttpContext context, IHttpClientFactory httpClientFactory, AppSettings appSettings)
        {
            var path = context.Request.Path.Value?.ToLower();
            _logger.LogWarning("AuthMiddleware HIT: {Path}", path);
            // Skip auth for login/logout/Register/VerifyOtp/ResendOtp
            if (path == null ||
                    path.Contains(RouteConstants.Login.ToLower()) ||
                    path.Contains(RouteConstants.Logout.ToLower()) ||
                    path.Contains(RouteConstants.Register.ToLower()) ||
                    path.Contains(RouteConstants.VerifyOtp.ToLower()) ||
                    path.Contains(RouteConstants.ResendOtp.ToLower()) ||
                    path.Contains(RouteConstants.ForgotPassword.ToLower()) ||
                    path.Contains(RouteConstants.ResetPassword.ToLower()) ||
                    path.StartsWith("/error") ||
                    path.Contains("/account/sendresetcode"))
            {
                await _next(context);
                return;
            }

            var accessToken = context.Session.GetString(SessionConstants.AccessToken);
            var refreshToken = context.Session.GetString(SessionConstants.RefreshToken);

            if (string.IsNullOrEmpty(accessToken))
            {
                context.Response.Redirect(RouteConstants.Login);
                return;
            }

            // Check if access token is about to expire (within 5 minutes)
            if (IsTokenExpiredOrExpiring(accessToken))
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshed = await RefreshTokenAsync(
                        httpClientFactory, appSettings, accessToken, refreshToken);

                    if (refreshed != null)
                    {
                        context.Session.SetString(SessionConstants.AccessToken, refreshed.AccessToken);
                        context.Session.SetString(SessionConstants.RefreshToken, refreshed.RefreshToken);
                        _logger.LogInformation("AuthMiddleware: Token refreshed silently.");
                    }
                    else
                    {
                        context.Session.Clear();
                        context.Response.Redirect(RouteConstants.Login);
                        return;
                    }
                }
                else
                {
                    context.Session.Clear();
                    context.Response.Redirect(RouteConstants.Login);
                    return;
                }
            }

            await _next(context);
        }
        #endregion

        #region IsTokenExpiredOrExpiring
        private bool IsTokenExpiredOrExpiring(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                var expiry = jwt.ValidTo;
                return expiry < DateTime.UtcNow.AddMinutes(5);
            }
            catch
            {
                return true;
            }
        }
        #endregion

        #region RefreshTokenAsync
        private async Task<TokenResponseModel?> RefreshTokenAsync(
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            string accessToken,
            string refreshToken)
        {
            try
            {
                var client = httpClientFactory.CreateClient("HealthCareApi");
                var endpoint = ApiSettings.RefreshTokenEndpoint(
                    appSettings.HealthCareApi.BaseUrl,
                    appSettings.HealthCareApi.Version);

                var payload = new { AccessToken = accessToken, RefreshToken = refreshToken };
                var json = JsonConvert.SerializeObject(payload);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(endpoint, httpContent);
                var content = await response.Content.ReadAsStringAsync();

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);
                if (apiResponse?.Status == true && apiResponse.Response != null)
                    return JsonConvert.DeserializeObject<TokenResponseModel>(
                        apiResponse.Response.ToString()!);

                return null;
            }
            catch
            {
                return null;
            }
        }
        #endregion
    }
}