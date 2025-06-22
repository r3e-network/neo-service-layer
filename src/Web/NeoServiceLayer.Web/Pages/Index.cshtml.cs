using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Neo Service Layer Web Interface accessed");
        }
    }
}
