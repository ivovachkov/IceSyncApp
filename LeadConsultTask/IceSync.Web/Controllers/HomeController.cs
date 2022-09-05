using AutoMapper;
using IceSync.ApiClient;
using IceSync.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace IceSync.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IApiClient _apiClient;
        private readonly IMapper _mapper;

        public HomeController(
            ILogger<HomeController> logger,
            IApiClient apiClient,
            IMapper mapper)
        {
            _logger = logger;
            _apiClient = apiClient;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Getting workflows from API Client...");

            var apiWorkflows = await _apiClient.GetWorkflows();
            var resultWorkflows = _mapper.Map<IEnumerable<ApiWorkflow>, IEnumerable<WorkflowViewModel>>(apiWorkflows);

            return View(resultWorkflows);
        }

        [HttpPost]
        public async Task<IActionResult> RunWorkflow([FromBody] int workflowId)
        {
            _logger.LogInformation($"Run workflow with id: {workflowId}");

            try
            {
                await _apiClient.RunWorkflow(workflowId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running workflow with id: {workflowId}");
                return Problem(ex.Message);
            }

            return Ok();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}