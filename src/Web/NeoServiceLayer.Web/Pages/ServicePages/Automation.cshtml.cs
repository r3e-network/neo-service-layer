using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class AutomationModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("automation");
    }
}
