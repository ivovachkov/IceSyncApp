using AutoMapper;
using Hangfire;
using IceSync.ApiClient;
using IceSync.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace IceSync.Web.Services
{
    public class IceSyncHostedService : IHostedService
    {
        private readonly ILogger<IceSyncHostedService> _logger;
        private readonly IRecurringJobManager _recurringJob;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public IceSyncHostedService(
            ILogger<IceSyncHostedService> logger,
            IRecurringJobManager recurringJob,
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper,
            IConfiguration configuration)
        {
            _logger = logger;
            _recurringJob = recurringJob;
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
            _configuration = configuration;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ice Sync Hosted Service running.");            

            using var scope = _serviceScopeFactory.CreateScope();

            var data = scope.ServiceProvider.GetService<DbContext>();

            if (data != null && !data.Database.CanConnect())
            {
                await data.Database.EnsureCreatedAsync();
            }

            _recurringJob.AddOrUpdate(
                nameof(IceSyncHostedService),
                () => ProcessSync(),
                _configuration.GetValue<string>("SyncJobSchedule"));
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ice Sync Hosted Service is stopping.");

            return Task.CompletedTask;
        }

        public async Task ProcessSync()
        {
            _logger.LogInformation("ProcessSync() from Ice Sync Hosted Service is starting.");
            
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var data = scope.ServiceProvider.GetService<DbContext>();
                var dbWorkflows = data.Set<Workflow>().ToDictionary(x => x.WorkflowId);
                var changeDb = false;

                var apiClient = scope.ServiceProvider.GetService<IApiClient>();
                var apiWorkflows = await apiClient.GetWorkflows();

                using var transaction = await data.Database.BeginTransactionAsync();

                foreach (var wf in apiWorkflows)
                {
                    if (!dbWorkflows.ContainsKey(wf.WorkflowId))
                    {
                        changeDb = true;
                        var entity = _mapper.Map<ApiWorkflow, Workflow>(wf);
                        await data.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Workflows ON");
                        data.Add(entity);
                    }
                    else if (
                        dbWorkflows[wf.WorkflowId].IsRunning != wf.IsRunning ||
                        dbWorkflows[wf.WorkflowId].IsActive != wf.IsActive ||
                        dbWorkflows[wf.WorkflowId].WorkflowName != wf.WorkflowName ||
                        dbWorkflows[wf.WorkflowId].MultiExecBehavior != wf.MultiExecBehavior)
                    {
                        changeDb = true;
                        dbWorkflows[wf.WorkflowId].IsRunning = wf.IsRunning;
                        dbWorkflows[wf.WorkflowId].IsActive = wf.IsActive;
                        dbWorkflows[wf.WorkflowId].WorkflowName = wf.WorkflowName;
                        dbWorkflows[wf.WorkflowId].MultiExecBehavior = wf.MultiExecBehavior;
                    }
                }

                var apiWfIds = apiWorkflows.Select(x => x.WorkflowId).ToHashSet();
                var unnecessaryDbWorkflows = dbWorkflows?.Values.Where(x => !apiWfIds.Contains(x.WorkflowId));

                if (unnecessaryDbWorkflows?.Count() > 0)
                {
                    changeDb = true;
                    data.RemoveRange(unnecessaryDbWorkflows);
                }

                if (changeDb)
                {
                    await data.SaveChangesAsync();
                    await data.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Workflows OFF");
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing workflows...");
                throw;
            }       
        }
    }
}