using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class BackupModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("backup");
    }
}
