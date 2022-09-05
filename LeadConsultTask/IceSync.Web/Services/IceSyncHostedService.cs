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

        public IceSyncHostedService(
            ILogger<IceSyncHostedService> logger,
            IRecurringJobManager recurringJob,
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper)
        {
            _logger = logger;
            _recurringJob = recurringJob;
            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
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
                "*/1 * * * *");
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ice Sync Hosted Service is stopping.");

            return Task.CompletedTask;
        }

        public async Task ProcessSync()
        {
            _logger.LogInformation("ProcessSync() from Ice Sync Hosted Service is starting.");

            using var scope = _serviceScopeFactory.CreateScope();           

            var data = scope.ServiceProvider.GetService<DbContext>();

            var dbWorkflows = data.Set<Workflow>().ToList();

            var dbChanges = false;
                        
            using var transaction = await data.Database.BeginTransactionAsync();

            var apiClient = scope.ServiceProvider.GetService<IApiClient>();
            var apiWorkflows = await apiClient.GetWorkflows();

            foreach (var wf in apiWorkflows)
            {
                var dbWf = dbWorkflows.FirstOrDefault(x => x.WorkflowId == wf.WorkflowId);
                if (dbWf == null)
                {
                    dbChanges = true;
                    var entity = _mapper.Map<ApiWorkflow, Workflow>(wf);
                    await data.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Workflows ON");
                    data.Add(entity);
                }
                else if (
                    dbWf.IsRunning != wf.IsRunning || 
                    dbWf.IsActive != wf.IsActive ||
                    dbWf.WorkflowName != wf.WorkflowName ||
                    dbWf.MultiExecBehavior != wf.MultiExecBehavior)
                {
                    dbChanges = true;
                    dbWf.IsRunning = wf.IsRunning;
                    dbWf.IsActive = wf.IsActive;
                    dbWf.WorkflowName = wf.WorkflowName;
                    dbWf.MultiExecBehavior = wf.MultiExecBehavior;
                }
            }

            var apiWfIds = apiWorkflows.Select(x => x.WorkflowId).ToHashSet();
            var unnecessaryDbWorkflows = dbWorkflows.Where(x => !apiWfIds.Contains(x.WorkflowId));

            if (unnecessaryDbWorkflows?.Count() > 0)
            {
                dbChanges = true;
                data.RemoveRange(unnecessaryDbWorkflows);
            }

            if (dbChanges)
            {                
                await data.SaveChangesAsync();
                await data.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Workflows OFF");
                transaction.Commit();
            } 
        }
    }
}