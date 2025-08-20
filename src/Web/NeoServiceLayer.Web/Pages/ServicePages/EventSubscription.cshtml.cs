using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Pages.ServicePages;

public class EventSubscriptionModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("eventsubscription");
    }
}
