using Aurelyn.Portal.Configurations;
using Aurelyn.Portal.Constants;
using Newtonsoft.Json;
using Serilog;
using System.Net.Http.Headers;
using System.Text;

namespace Ecommerce.Web.Services.Base
{
    public abstract class BaseService
    {
        #region Properties
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        protected string AurelynApiUrl =>
            _appSettings.HealthCareApi.BaseUrl;

        protected string AurelynApiVersion =>
            _appSettings.HealthCareApi.Version;

        #endregion

        #region Constructor
        protected BaseService(
            IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings;
            _httpContextAccessor = httpContextAccessor;
        }

        #endregion

        #region GetHttpClient
        private HttpClient GetHttpClient()
        {
            var client = _httpClientFactory.CreateClient("HealthCareApi");

            var token = _httpContextAccessor.HttpContext?.Session
                .GetString(SessionConstants.AccessToken);

            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        #endregion

        #region DoHttpGet
        protected async Task<HttpResponseMessage?> DoHttpGet(
            string endpoint, bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                return await client.GetAsync(endpoint);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region DoHttpPut
        protected async Task<T?> DoHttpPut<T>(
            string endpoint, object payload, bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                var json = JsonConvert.SerializeObject(payload);

                using var httpContent =
                    new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await client.PutAsync(endpoint, httpContent);

                if (!response.IsSuccessStatusCode)
                    return default;

                var content = await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(content);
            }
            catch
            {
                return default;
            }
        }

        #endregion

        #region DoHttpPost
        protected async Task<HttpResponseMessage?> DoHttpPost(
            string endpoint, object payload, bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                var json = JsonConvert.SerializeObject(payload);

                using var httpContent =
                    new StringContent(json, Encoding.UTF8, "application/json");

                return await client.PostAsync(endpoint, httpContent);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "DoHttpPost error. Endpoint: {Endpoint}", endpoint);
                return null;
            }
        }
        #endregion

        #region DoHttpDelete
        protected async Task<HttpResponseMessage?> DoHttpDelete(
            string endpoint, bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                return await client.DeleteAsync(endpoint);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region DoHttpPostFile
        protected async Task<HttpResponseMessage?> DoHttpPostFile(
            string endpoint,
            IFormFile file,
            string fieldName = "file",
            bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                using var content = new MultipartFormDataContent();
                using var fileStream = file.OpenReadStream();
                using var streamContent = new StreamContent(fileStream);

                streamContent.Headers.ContentType =
                    new MediaTypeHeaderValue(
                        string.IsNullOrEmpty(file.ContentType)
                            ? "application/octet-stream"
                            : file.ContentType);

                content.Add(streamContent, fieldName, file.FileName);

                return await client.PostAsync(endpoint, content);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region DoHttpPostNoBody
        protected async Task<HttpResponseMessage?> DoHttpPostNoBody(
            string endpoint, bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                return await client.PostAsync(endpoint, null);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        //#region ApiResponse
        //protected async Task<Aurelyn.Portal.Models.Common.ApiResponse> ApiResponse(
        //    HttpResponseMessage? response)
        //{
        //    try
        //    {
        //        if (response == null)
        //        {
        //            return new Aurelyn.Portal.Models.Common.ApiResponse
        //            {
        //                Status = false,
        //                Message = "Unable to connect to API."
        //            };
        //        }

        //        var content = await response.Content.ReadAsStringAsync();

        //        var apiResponse =
        //            JsonConvert.DeserializeObject<Aurelyn.Portal.Models.Common.ApiResponse>(content);

        //        return apiResponse ?? new Aurelyn.Portal.Models.Common.ApiResponse
        //        {
        //            Status = false,
        //            Message = "Invalid API response."
        //        };
        //    }
        //    catch (Exception)
        //    {
        //        return new Aurelyn.Portal.Models.Common.ApiResponse
        //        {
        //            Status = false,
        //            Message = "Error while processing API response."
        //        };
        //    }
        //}
        //#endregion
    }
}