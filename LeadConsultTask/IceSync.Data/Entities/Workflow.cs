namespace IceSync.Data.Entities
{
    public class Workflow
    {
        public int WorkflowId { get; set; }
        public string WorkflowName { get; set; }
        public bool IsActive { get; set; }
        public bool IsRunning { get; set; }
        public string? MultiExecBehavior { get; set; }
    }
}
