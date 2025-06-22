using Microsoft.AspNetCore.Mvc.RazorPages;

namespace NeoServiceLayer.Web.Pages.ServicePages;

public class ProofOfReserveModel : TemplateModel
{
    public void OnGet()
    {
        base.OnGet("proofofreserve");
    }
}
