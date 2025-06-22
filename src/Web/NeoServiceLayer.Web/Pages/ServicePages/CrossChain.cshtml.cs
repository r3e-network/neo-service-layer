using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class CrossChainModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("crosschain");
    }
}
