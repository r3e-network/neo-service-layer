using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ComplianceModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("compliance");
    }
}
