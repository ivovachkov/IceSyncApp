using Hangfire;
using Hangfire.SqlServer;
using IceSync.ApiClient;
using IceSync.Data;
using IceSync.Web.Extensions;
using IceSync.Web.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data.SqlClient;

namespace IceSync.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();
            Log.Information("Starting up");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseSerilog((ctx, lc) => lc
                    .WriteTo.Console()
                    .ReadFrom.Configuration(ctx.Configuration));

                //builder.Services
                //    .ConfigureHangfire(builder.Configuration)
                //    .ConfigureServices(builder.Configuration);

                // Add services to the container.
                builder.Services.AddControllersWithViews();
                builder.Services.AddHttpClient();
                builder.Services.AddOptions<ApiClientConfiguration>()
                    .Bind(builder.Configuration.GetSection(nameof(ApiClientConfiguration)));
                builder.Services.AddScoped<IApiClient, IceSync.ApiClient.ApiClient>();
                builder.Services.AddScoped<DbContext, WorkflowDbContext>();
                builder.Services.AddDbContext<WorkflowDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("WorkflowDatabase")));

                // Hangfire
                CreateHangfireDatabase(builder.Configuration);
                builder.Services.AddHangfire(config =>
                {
                    config
                        .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                        .UseSimpleAssemblyNameTypeSerializer()
                        .UseRecommendedSerializerSettings()
                        .UseSqlServerStorage(
                            builder.Configuration.GetConnectionString("HangfireDatabase"),
                            new SqlServerStorageOptions
                            {
                                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                                QueuePollInterval = TimeSpan.Zero,
                                UseRecommendedIsolationLevel = true,
                                DisableGlobalLocks = true // Migration to Schema 7 is required
                            });
                });
                builder.Services.AddHangfireServer();

                builder.Services.AddHostedService<IceSyncHostedService>();
                builder.Services.AddAutoMapper(typeof(Program));

                var app = builder.Build();

                //app.InitializeDb().ConfigureApp();

                // Initialize Db
                using var serviceScope = app.Services.CreateScope();
                var serviceProvider = serviceScope.ServiceProvider;
                var db = serviceProvider.GetRequiredService<DbContext>();
                db.Database.EnsureCreated();

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
                app.UseHangfireDashboard();
                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception");
            }
            finally
            {
                Log.Information("Shut down complete");
                Log.CloseAndFlush();
            }
        }

        private static void CreateHangfireDatabase(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("HangfireDatabase");
            var dbName = connectionString.Split(";")[1].Split("=")[1];

            using var connection = new SqlConnection(connectionString.Replace(dbName, "master"));
            connection.Open();
            using var command = new SqlCommand(
                $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{dbName}') create database [{dbName}];",
                connection);
            command.ExecuteNonQuery();
        }
    }
}