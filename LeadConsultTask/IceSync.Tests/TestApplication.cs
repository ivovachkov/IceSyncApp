using IceSync.ApiClient;
using IceSync.Web;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IceSync.Tests
{
    internal class TestApplication : WebApplicationFactory<Program>
    {
        public IConfiguration Configuration { get; }

        public TestApplication()
        {
            Configuration = BuildConfigurationRoot();
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(c =>
            {
                c.Sources.Clear();
                c.AddJsonFile("appsettings.inttests.json");
            });
            builder.ConfigureServices(s =>
            {
                s.AddHttpClient();
                s.AddOptions<ApiClientConfiguration>()
                    .Bind(Configuration.GetSection(nameof(ApiClientConfiguration)));
                s.AddScoped<IApiClient, IceSync.ApiClient.ApiClient>();
            });

            return base.CreateHost(builder);
        }

        private static IConfigurationRoot BuildConfigurationRoot()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.inttests.json")
                .Build();
            return configuration;
        }
    }
}