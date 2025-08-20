using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;


namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ConfigurationModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("configuration");
    }
}
