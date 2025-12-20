namespace WhatsAppSender.API
{
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                            p.ParameterType == typeof(IFormFileCollection));

            if (!fileParams.Any())
                return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = fileParams.ToDictionary(
                            p => p.Name ?? "file",
                            p => new OpenApiSchema
                            {
                                Type = "string",
                                Format = "binary"
                            })
                    }
                }
            }
            };
        }
    }

}
