using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace IceSync.ApiClient
{
    public class ApiClient : IApiClient
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiClientConfiguration _apiClientConfiguration;

        public ApiClient(
            IMemoryCache memoryCache,
            IHttpClientFactory httpClientFactory,
            IOptions<ApiClientConfiguration> apiClientConfiguration)
        {
            _memoryCache = memoryCache;
            _httpClientFactory = httpClientFactory;
            _apiClientConfiguration = apiClientConfiguration.Value;
        }

        public async Task<IEnumerable<ApiWorkflow>> GetWorkflows()
        {
            var apiClient = await GetApiClient();
            var response = await apiClient.GetAsync("workflows");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {                
                throw new NotSupportedException(responseContent);
            }

            var result = JsonConvert.DeserializeObject<IEnumerable<ApiWorkflow>>(responseContent);

            return result.OrderBy(x => x.WorkflowId).ToList();
        }

        public async Task RunWorkflow(int workflowId)
        {
            var apiClient = await GetApiClient();
            var response = await apiClient.PostAsync($"workflows/{workflowId}/run", null);

            if (!response.IsSuccessStatusCode)
            {
                throw new NotSupportedException($"Error trying to run workflow with id: {workflowId}");
            }
        }

        public async Task<HttpClient> GetApiClient()
        {
            var token = await GetTokenAsync();

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(_apiClientConfiguration.BaseUri);
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return httpClient;
        }

        public async Task<string> GetTokenAsync()
        {
            return await _memoryCache.GetOrCreateAsync("ApiClient-Token", async e =>
            {
                var client = _httpClientFactory.CreateClient();

                var token = await GetTokenFromApiAsync(client);

                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                {
                    throw new ArgumentException("Invalid jwt token.");
                }

                var jwtSecurityToken = handler.ReadJwtToken(token);

                var expirationTime = jwtSecurityToken.ValidTo - DateTime.UtcNow;

                //if expiration time is not returned for some reason, at least some caching
                if (expirationTime < TimeSpan.FromSeconds(60))
                {
                    expirationTime = TimeSpan.FromSeconds(60);
                }
                else
                {
                    //some buffer to avoid use of expired tokens
                    expirationTime -= TimeSpan.FromSeconds(15);
                }

                e.AbsoluteExpirationRelativeToNow = expirationTime;

                return token;
            });
        }

        private async Task<string> GetTokenFromApiAsync(HttpClient client)
        {
            var tokenResponse = await client.PostAsJsonAsync(_apiClientConfiguration.AuthUri, _apiClientConfiguration);

            var responseContent = await tokenResponse.Content.ReadAsStringAsync();

            if (!tokenResponse.IsSuccessStatusCode)
            {
                throw new NotSupportedException(responseContent);
            }

            return responseContent;
        }
    }
}