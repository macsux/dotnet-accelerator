using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyProjectGroup.Common.Swagger;

public static class SwaggerGenOptionsExtensions
{
    public static SwaggerGenOptions UseOperationIdsConventions(this SwaggerGenOptions options)
    {
        options.CustomOperationIds(api =>
        {
            var actionDescriptor = (ControllerActionDescriptor) api.ActionDescriptor;
            HttpMethodAttribute? methodAttribute = api.HttpMethod switch
            {
                "GET" => actionDescriptor.EndpointMetadata.OfType<HttpGetAttribute>().FirstOrDefault(),
                "POST" =>  actionDescriptor.EndpointMetadata.OfType<HttpPostAttribute>().FirstOrDefault(),
                "PUT" =>  actionDescriptor.EndpointMetadata.OfType<HttpPutAttribute>().FirstOrDefault(),
                "DELETE" =>  actionDescriptor.EndpointMetadata.OfType<HttpDeleteAttribute>().FirstOrDefault(),
                "PATCH" =>  actionDescriptor.EndpointMetadata.OfType<HttpPatchAttribute>().FirstOrDefault(),
                "OPTIONS" =>  actionDescriptor.EndpointMetadata.OfType<HttpOptionsAttribute>().FirstOrDefault(),
                "HEAD" =>  actionDescriptor.EndpointMetadata.OfType<HttpHeadAttribute>().FirstOrDefault(),
                _ => null!
            };
            if (methodAttribute?.Name is not null)
            {
                return methodAttribute.Name;
            }

            return $"{actionDescriptor.ControllerName}_{actionDescriptor.ActionName}"; 

            // return $"{((ControllerActionDescriptor) api.ActionDescriptor).ControllerName}_{api.HttpMethod}_{string.Join("_", api.ParameterDescriptions.Select(x => x.Name))}".ToLower();
        });
        return options;
    }
}