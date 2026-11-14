
using Aurelyn.Portal.Configurations;
using Aurelyn.Portal.Models.ChangePasswordViewModel;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Models.User;
using Ecommerce.Web.Services.Base;
using Newtonsoft.Json;
using Serilog;

namespace Aurelyn.Portal.Service.User
{
    #region Interface
    public interface IUserService
    {
        Task<UserDetails?> GetProfileAsync();
        Task<ApiResponse> ChangePasswordAsync(ChangePasswordViewModel model);
    }
    #endregion

    public class UserService : BaseService, IUserService
    {
        #region Constructor
        public UserService(
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            IHttpContextAccessor httpContextAccessor)
            : base(httpClientFactory, appSettings, httpContextAccessor)
        {
        }
        #endregion

        #region GetProfileAsync
        public async Task<UserDetails?> GetProfileAsync()
        {
            try
            {
                var endpoint = $"{AurelynApiUrl}/api/v{AurelynApiVersion}/user/me";

                var response = await DoHttpGet(endpoint, useAuth: true);

                if (response == null)
                    return null;

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Logger.Error(
                        "GetProfileAsync failed: {StatusCode} - {Content}",
                        response.StatusCode, content);

                    return null;
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);

                if (apiResponse?.Status == true && apiResponse.Response != null)
                {
                    return JsonConvert.DeserializeObject<UserDetails>(
                        apiResponse.Response.ToString()!);
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "UserService.GetProfileAsync error");
                return null;
            }
        }
        #endregion

        #region ChangePasswordAsync
        public async Task<ApiResponse> ChangePasswordAsync(
            ChangePasswordViewModel model)
        {
            try
            {
                var endpoint = $"{AurelynApiUrl}/api/v{AurelynApiVersion}/user/change-password";

                var payload = new
                {
                    CurrentPassword = model.CurrentPassword,
                    NewPassword = model.NewPassword,
                    ConfirmPassword = model.ConfirmPassword
                };

                var response = await DoHttpPost(
                    endpoint,
                    payload,
                    useAuth: true);

                if (response == null)
                {
                    return new ApiResponse
                    {
                        Status = false,
                        Message = "Unable to connect to API."
                    };
                }

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Log.Logger.Error(
                        "ChangePasswordAsync failed: {StatusCode} - {Content}",
                        response.StatusCode, content);
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);

                return apiResponse ?? new ApiResponse
                {
                    Status = false,
                    Message = "Invalid response"
                };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "UserService.ChangePasswordAsync error");

                return new ApiResponse
                {
                    Status = false,
                    Message = "Error occurred"
                };
            }
        }
        #endregion
    }
}
