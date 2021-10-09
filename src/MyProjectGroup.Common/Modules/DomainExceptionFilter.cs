using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyProjectGroup.Common.Modules
{
    public class DomainExceptionFilter : IActionFilter, IOrderedFilter
    {
        public int Order { get; } = int.MaxValue - 10;

        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var isDevelopment = context.HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
            if (context.Exception is DomainException exception)
            {
                context.Result = new ObjectResult(isDevelopment ? exception.ToString() : exception.Message)
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                };
                context.ExceptionHandled = true;
            }
        }
    }

}