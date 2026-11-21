using Aurelyn.Portal.Configurations;
using Aurelyn.Portal.Models.Account;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Models.User;
using Aurelyn.Portal.Service.Tenant;
using Ecommerce.Web.Services.Base;
using Newtonsoft.Json;
using Serilog;

namespace Aurelyn.Portal.Service.Account
{
    #region Interface

    public interface IAccountService
    {
        Task<ApiResponse> LoginAsync(LoginViewModel model);
        Task<ApiResponse> VerifyOtpAsync(long otpId, string code);
        Task<ApiResponse> ResendOtpAsync(long otpId);
        Task<ApiResponse> RegisterAsync(RegisterViewModel model);
        Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordViewModel model);
        Task<ApiResponse> ResetPasswordAsync(ResetPasswordViewModel model);
        Task<ApiResponse> RefreshTokenAsync(string accessToken, string refreshToken);
        Task LogoutAsync(string? refreshToken);
    }

    #endregion

    public class AccountService : BaseService, IAccountService
    {
        #region Properties

        private const string ClientApp = "Web";
        private readonly ITenantService _tenantService;

        #endregion

        #region Constructor
        public AccountService(
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            IHttpContextAccessor httpContextAccessor , ITenantService tenantService)
            : base(httpClientFactory, appSettings, httpContextAccessor)
        {
            _tenantService = tenantService;
        }

        #endregion

        #region LoginAsync

        public async Task<ApiResponse> LoginAsync(LoginViewModel model)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/access-token";

                var payload = new
                {
                    Email = model.Email,
                    Password = model.Password,
                    TenantCode = _tenantService.GetTenantCode(),
                    ClientApp = ClientApp
                };

                var response = await DoHttpPost(
                    endpoint, payload, useAuth: false);

                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse { Status = false, Message = "No response from server" };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "LoginAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region VerifyOtpAsync

        public async Task<ApiResponse> VerifyOtpAsync(long otpId, string code)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/verify-otp";

                var payload = new
                {
                    OtpId = otpId,
                    Code = code
                };
                var response = await DoHttpPost(endpoint, payload, useAuth: false);
                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse { Status = false, Message = "No response from server" };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "VerifyOtpAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region ResendOtpAsync

        public async Task<ApiResponse> ResendOtpAsync(long otpId)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/resend-otp";

                var payload = new
                {
                    OtpId = otpId,
                    ClientApp = ClientApp,
                    TenantCode = _tenantService.GetTenantCode(),
                };

                var response = await DoHttpPost(endpoint, payload, useAuth: false);
                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse { Status = false, Message = "No response from server" };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "ResendOtpAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region RegisterAsync

        public async Task<ApiResponse> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var endpoint = $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/register";

                var payload = new
                {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    TenantCode = _tenantService.GetTenantCode(),
                    ClientApp = ClientApp
                };

                var response = await DoHttpPost(endpoint, payload, useAuth: false);

                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse
                    {
                        Status = false,
                        Message = "No response from server"
                    };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "RegisterAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region ForgotPasswordAsync

        public async Task<ApiResponse> ForgotPasswordAsync(
            ForgotPasswordViewModel model)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/forgot-password";

                var payload = new
                {
                    Email = model.Email,
                    TenantCode = _tenantService.GetTenantCode(),
                    ClientApp = ClientApp
                };

                var response = await DoHttpPost(endpoint, payload, useAuth: false);

                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };
                var content = await response.Content.ReadAsStringAsync();

                // no EnsureSuccessStatusCode: read the API's own message even for 400 or 409
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse ();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "ForgotPasswordAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region ResetPasswordAsync

        public async Task<ApiResponse> ResetPasswordAsync(
            ResetPasswordViewModel model)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/reset-password";

                var payload = new
                {
                    Email = model.Email,
                    TenantCode = _tenantService.GetTenantCode(),
                    Code = model.Code,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                };

                var response = await DoHttpPost(endpoint, payload, useAuth: false);
                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse { Status = false, Message = "No response from server" };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "ResetPasswordAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region RefreshTokenAsync

        public async Task<ApiResponse> RefreshTokenAsync(
            string accessToken, string refreshToken)
        {
            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/refresh-token";

                var payload = new
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };

                var response = await DoHttpPost(endpoint, payload, useAuth: false);
                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content)
                    ?? new ApiResponse { Status = false, Message = "No response from server" };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "RefreshTokenAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }

        #endregion

        #region LogoutAsync

        public async Task LogoutAsync(string? refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return;

            try
            {
                var endpoint =
                    $"{AurelynApiUrl}/api/v{AurelynApiVersion}/token/logout";

                var payload = new
                {
                    RefreshToken = refreshToken
                };

                var response = await DoHttpPost(
                    endpoint, payload, useAuth: true);
                if (response == null) return;
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "LogoutAsync error");
            }
        }

        #endregion
    }
}