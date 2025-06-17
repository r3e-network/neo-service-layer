using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class MonitoringModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("monitoring");
    }
} 