using AutoMapper;
using IceSync.ApiClient;
using IceSync.Data.Entities;
using IceSync.Web.Models;

namespace IceSync.Web.Profiles
{
    public class WorkflowProfile : Profile
    {
        public WorkflowProfile()
        {
            CreateMap<ApiWorkflow, Workflow>();
            CreateMap<ApiWorkflow, WorkflowViewModel>();
        }
    }
}
