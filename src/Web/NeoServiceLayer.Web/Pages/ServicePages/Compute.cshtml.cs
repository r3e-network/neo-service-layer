using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ComputeModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("compute");
    }
} 