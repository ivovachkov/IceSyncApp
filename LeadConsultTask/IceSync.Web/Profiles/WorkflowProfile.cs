namespace WebApplication2.Profiles
{
    using AutoMapper;
    using IceSync.ApiClient;
    using IceSync.Data.Entities;
    using IceSync.Web.Models;

    public class WorkflowProfile : Profile
    {
        public WorkflowProfile()
        {
            CreateMap<ApiWorkflow, Workflow>();
            CreateMap<ApiWorkflow, WorkflowViewModel>();
        }
    }
}
