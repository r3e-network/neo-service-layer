using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace NeoServiceLayer.Api.Controllers.V2
{
    /// <summary>
    /// Base API controller for v2 endpoints with common functionality.
    /// </summary>
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Produces("application/json")]
    public abstract class BaseApiControllerV2 : BaseApiController
    {
        /// <summary>
        /// Gets the API version.
        /// </summary>
        protected string ApiVersion => "2.0";
    }
}
