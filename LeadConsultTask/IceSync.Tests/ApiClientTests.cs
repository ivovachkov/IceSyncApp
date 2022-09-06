using IceSync.ApiClient;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace IceSync.Tests
{
    public class ApiClientTests
    {
        private readonly ITestOutputHelper _output;
        private readonly TestApplication _app;

        public ApiClientTests(ITestOutputHelper output)
        {
            _output = output;
            _app = new TestApplication();
        }

        [Fact]
        public async Task GetWorfkflows()
        {
            var apiClient = _app.Services.GetRequiredService<IApiClient>();

            var workflows = await apiClient.GetWorkflows();


        }
    }
}