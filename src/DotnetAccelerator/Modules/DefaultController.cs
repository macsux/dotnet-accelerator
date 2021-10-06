using Microsoft.AspNetCore.Mvc;

namespace DotnetAccelerator.Modules
{
    [Controller]
    public class DefaultController : ControllerBase
    {
        [HttpGet("/")]
        public void Get()
        {
            HttpContext.Response.Redirect("swagger");
        }
    }
}