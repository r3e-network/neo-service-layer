using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class EventSubscriptionModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("eventsubscription");
    }
}
