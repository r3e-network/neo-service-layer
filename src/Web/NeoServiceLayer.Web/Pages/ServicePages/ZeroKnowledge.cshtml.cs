using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ZeroKnowledgeModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("zeroknowledge");
    }
}
