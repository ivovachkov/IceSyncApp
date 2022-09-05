using Newtonsoft.Json;

namespace IceSync.ApiClient
{
    public class ApiWorkflow
    {
        [JsonProperty("id")]
        public int WorkflowId { get; set; }

        [JsonProperty("name")]
        public string WorkflowName { get; set; }

        [JsonProperty("isActive")]
        public bool IsActive { get; set; }

        [JsonProperty("isRunning")]
        public bool IsRunning { get; set; }

        [JsonProperty("multiExecBehavior")]
        public string? MultiExecBehavior { get; set; }
    }
}