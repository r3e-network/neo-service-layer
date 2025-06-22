using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ConfigurationModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("configuration");
    }
}
