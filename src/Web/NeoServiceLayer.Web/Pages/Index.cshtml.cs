using System.Reflection;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(ILogger<IndexModel> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public string Environment { get; private set; } = string.Empty;
        public string Version { get; private set; } = string.Empty;
        public int ServiceCount { get; private set; }
        public string Uptime { get; private set; } = string.Empty;
        public int TestCoverage { get; private set; }

        public void OnGet()
        {
            _logger.LogInformation("Neo Service Layer Web Interface accessed");

            // Set page model properties
            Environment = _environment.EnvironmentName;
            Version = GetVersion();
            ServiceCount = 30; // Total number of services
            Uptime = "99.9%";
            TestCoverage = 85;
        }

        private string GetVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
            }
            catch
            {
                return "1.0.0";
            }
        }
    }
}
