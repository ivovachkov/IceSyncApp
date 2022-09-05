using Hangfire;
using Hangfire.SqlServer;
using IceSync.ApiClient;
using IceSync.Data;
using IceSync.Web.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.SqlClient;

namespace IceSync.Web
{
    public static class Extensions
	{
		public static IServiceCollection ConfigureServices(this IServiceCollection services, ConfigurationManager configuration)
		{
            services.AddControllersWithViews();
            services.AddHttpClient();
            services.AddOptions<ApiClientConfiguration>()
                .Bind(configuration.GetSection(nameof(ApiClientConfiguration)));
            services.AddScoped<IApiClient, IceSync.ApiClient.ApiClient>();
            services.AddScoped<DbContext, WorkflowDbContext>();
            services.AddDbContext<WorkflowDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("WorkflowDatabase")));

            // Hangfire
            CreateHangfireDatabase(configuration);
            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    .UseSqlServerStorage(
                        configuration.GetConnectionString("HangfireDatabase"),
                        new SqlServerStorageOptions
                        {
                            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                            QueuePollInterval = TimeSpan.Zero,
                            UseRecommendedIsolationLevel = true,
                            DisableGlobalLocks = true // Migration to Schema 7 is required
                        });
            });
            services.AddHangfireServer();
            
            services.AddHostedService<IceSyncHostedService>();
            
            services.AddAutoMapper(typeof(Program));

            return services;
        }

        public static WebApplication ConfigureApp(this WebApplication app)
        {
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseHangfireDashboard();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            return app;
        }

        public static WebApplication Initialize(this WebApplication app)
        {
            using var serviceScope = app.Services.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;

            var db = serviceProvider.GetRequiredService<DbContext>();
            db.Database.EnsureCreated();

            return app;
        }

        private static void CreateHangfireDatabase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("HangfireDatabase");

            var dbName = connectionString
                .Split(";")[1]
                .Split("=")[1];

            using var connection = new SqlConnection(connectionString.Replace(dbName, "master"));

            connection.Open();

            using var command = new SqlCommand(
                $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}') create database [{dbName}];",
                connection);

            command.ExecuteNonQuery();
        }
    }
}
