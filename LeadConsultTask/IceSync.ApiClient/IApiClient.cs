namespace IceSync.ApiClient
{
    public interface IApiClient
    {
        Task<HttpClient> GetApiClient();
        Task<string> GetTokenAsync();
        Task<IEnumerable<ApiWorkflow>> GetWorkflows();
        Task RunWorkflow(int workflowId);
    }
}