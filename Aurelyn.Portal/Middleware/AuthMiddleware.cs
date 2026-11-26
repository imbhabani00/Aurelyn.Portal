
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
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthMiddleware> _logger;

        public AuthMiddleware(
            RequestDelegate next,
            ILogger<AuthMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant();

            _logger.LogInformation(
                "AuthMiddleware HIT: {Path}", path);

            // Public routes
            if (path == null ||
                path.Contains(RouteConstants.Login.ToLowerInvariant()) ||
                path.Contains(RouteConstants.Logout.ToLowerInvariant()) ||
                path.Contains(RouteConstants.Register.ToLowerInvariant()) ||
                path.Contains(RouteConstants.VerifyOtp.ToLowerInvariant()) ||
                path.Contains(RouteConstants.ResendOtp.ToLowerInvariant()) ||
                path.Contains(RouteConstants.ForgotPassword.ToLowerInvariant()) ||
                path.Contains(RouteConstants.ResetPassword.ToLowerInvariant()) ||
                path.StartsWith("/error") ||
                path.Contains("/account/sendresetcode"))
            {
                await _next(context);
                return;
            }

            var accessToken = context.Session.GetString(
                SessionConstants.AccessToken);

            var refreshToken = context.Session.GetString(
                SessionConstants.RefreshToken);

            // Access token missing
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning(
                    "Access token missing for {Path}", path);

                await HandleUnauthorized(context);
                return;
            }

            // Check expiry
            if (IsTokenExpiredOrExpiring(accessToken))
            {
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var refreshed = await RefreshTokenAsync(
                        httpClientFactory,
                        appSettings,
                        accessToken,
                        refreshToken);

                    if (refreshed != null &&
                        !string.IsNullOrWhiteSpace(refreshed.AccessToken))
                    {
                        context.Session.SetString(
                            SessionConstants.AccessToken,
                            refreshed.AccessToken);

                        if (!string.IsNullOrWhiteSpace(
                            refreshed.RefreshToken))
                        {
                            context.Session.SetString(
                                SessionConstants.RefreshToken,
                                refreshed.RefreshToken);
                        }

                        _logger.LogInformation(
                            "AuthMiddleware: Token refreshed successfully.");
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Token refresh failed for {Path}", path);

                        context.Session.Clear();

                        await HandleUnauthorized(context);
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Refresh token missing for {Path}", path);

                    context.Session.Clear();

                    await HandleUnauthorized(context);
                    return;
                }
            }

            await _next(context);
        }

        private static async Task HandleUnauthorized(
            HttpContext context)
        {
            bool isAjax =
                context.Request.Headers["X-Requested-With"]
                == "XMLHttpRequest";

            if (isAjax)
            {
                context.Response.StatusCode =
                    StatusCodes.Status401Unauthorized;

                context.Response.ContentType =
                    "application/json";

                await context.Response.WriteAsJsonAsync(new
                {
                    status = false,
                    message = "Your session has expired. Please login again."
                });

                return;
            }

            context.Response.Redirect(RouteConstants.Login);
        }

        private bool IsTokenExpiredOrExpiring(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);

                return jwt.ValidTo < DateTime.UtcNow.AddMinutes(5);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex, "Unable to read access token expiry.");

                return true;
            }
        }

        private async Task<TokenResponseModel?> RefreshTokenAsync(
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            string accessToken,
            string refreshToken)
        {
            try
            {
                var client =
                    httpClientFactory.CreateClient("HealthCareApi");

                var endpoint = ApiSettings.RefreshTokenEndpoint(
                    appSettings.HealthCareApi.BaseUrl,
                    appSettings.HealthCareApi.Version);

                var payload = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var json = JsonConvert.SerializeObject(payload);

                var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    endpoint, content);

                var responseContent =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Refresh API returned HTTP {StatusCode}",
                        (int)response.StatusCode);

                    return null;
                }

                var apiResponse =
                    JsonConvert.DeserializeObject<ApiResponse>(
                        responseContent);

                if (apiResponse?.Status == true &&
                    apiResponse.Response != null)
                {
                    return JsonConvert.DeserializeObject<TokenResponseModel>(
                        apiResponse.Response.ToString()!);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex, "RefreshTokenAsync failed.");

                return null;
            }
        }
    }
}
