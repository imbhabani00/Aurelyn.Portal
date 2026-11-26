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
        Task<ApiResponse> UpdateProfileAsync(UserProfileUpdateRequest mdodel);
        Task<ApiResponse> UpdateProfilePictureAsync(IFormFile? file);
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
                Log.Logger.Information("GetProfile: calling {Endpoint}", endpoint);

                var response = await DoHttpGet(endpoint, useAuth: true);

                if (response == null)
                {
                    Log.Logger.Error("GetProfile NULL #1: DoHttpGet returned null (token missing in session, or request threw)");
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                Log.Logger.Information("GetProfile: HTTP {Status}, body: {Body}", (int)response.StatusCode, content);

                if (!response.IsSuccessStatusCode)
                {
                    Log.Logger.Error("GetProfile NULL #2: non-success status {Status}", (int)response.StatusCode);
                    return null;
                }

                var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(content);

                if (apiResponse == null || apiResponse.Status != true)
                {
                    Log.Logger.Error("GetProfile NULL #3: ApiResponse null or Status false. Message: {Msg}", apiResponse?.Message);
                    return null;
                }

                if (apiResponse.Response == null)
                {
                    Log.Logger.Error("GetProfile NULL #4: Response payload is null");
                    return null;
                }

                return JsonConvert.DeserializeObject<UserDetails>(apiResponse.Response.ToString()!);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "GetProfile NULL #5: exception");
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

        #region UpdateProfileAsync
        public async Task<ApiResponse> UpdateProfileAsync(UserProfileUpdateRequest mdodel)
        {
            var data = new ApiResponse();
            try
            {
                var endpoint = $"{AurelynApiUrl}/api/v{AurelynApiVersion}/user/profile-update";
                var response = await DoHttpPost(endpoint, mdodel, useAuth: true);
                if(response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<ApiResponse>(content);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Profile update error");
            }
            return data;
        }
        #endregion

        #region UpdateProfilePictureAsync
        public async Task<ApiResponse> UpdateProfilePictureAsync(IFormFile? file)
        {
            try
            {
                var endpoint = $"{AurelynApiUrl}/api/v{AurelynApiVersion}/user/profile-picture-update";

                var response = await DoHttpPostMultipart(endpoint, file, "file", useAuth: true);
                if (response == null)
                    return new ApiResponse { Status = false, Message = "Unable to reach the server." };

                var body = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(body))
                    return new ApiResponse { Status = false, Message = $"Server returned {(int)response.StatusCode} with no content." };

                return JsonConvert.DeserializeObject<ApiResponse>(body)
                       ?? new ApiResponse { Status = false, Message = "Invalid server response." };
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Profile picture update error");
                return new ApiResponse { Status = false, Message = "An error occurred. Please try again." };
            }   
        }
        #endregion
    }
}