using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages
{
    public class SmartContractsModel : PageModel
    {
        private readonly ILogger<SmartContractsModel> _logger;

        public SmartContractsModel(ILogger<SmartContractsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Smart Contracts service page accessed");
        }
    }
}
