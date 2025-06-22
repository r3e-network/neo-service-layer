using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class NotificationModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("notification");
    }
}
