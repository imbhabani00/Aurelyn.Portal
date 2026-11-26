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
            var context = _httpContextAccessor.HttpContext;

            if (context == null)
                throw new InvalidOperationException("HttpContext is missing.");

            var token = context.Session.GetString(
                SessionConstants.AccessToken);

            if (string.IsNullOrWhiteSpace(token))
            {
                Log.Logger.Error("API request failed: Access token missing from Session.");
                throw new UnauthorizedAccessException(
                    "Access token not found in Session.");
            }

            var client = _httpClientFactory.CreateClient("HealthCareApi");

            Log.Logger.Information(
                "API request: Session available, access token present. Token length: {Length}",
                token.Length);

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

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

                var response = await client.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    Log.Logger.Error(
                        "API GET failed. Endpoint: {Endpoint}, Status: {Status}, Response: {Response}",
                        endpoint,
                        (int)response.StatusCode,
                        content);
                }

                return response;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "DoHttpGet exception: {Endpoint}", endpoint);
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

        #region DoHttpPostMultipart
        protected async Task<HttpResponseMessage?> DoHttpPostMultipart(
            string endpoint,
            IFormFile? file,
            string fieldName = "file",
            bool useAuth = true)
        {
            try
            {
                var client = useAuth
                    ? GetHttpClient()
                    : _httpClientFactory.CreateClient("HealthCareApi");

                using var content = new MultipartFormDataContent();
                await using var fileStream = file?.OpenReadStream();

                if (file != null && file.Length > 0 && fileStream != null)
                {
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(
                        string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType);
                    content.Add(streamContent, fieldName, file.FileName);
                }
                else
                {
                    content.Add(new StringContent("true"), "remove");
                }

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint) { Content = content };

                return await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "DoHttpPostMultipart error. Endpoint: {Endpoint}", endpoint);
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
    }
}