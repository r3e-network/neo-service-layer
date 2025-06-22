using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class RandomnessModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("randomness");
    }
}
