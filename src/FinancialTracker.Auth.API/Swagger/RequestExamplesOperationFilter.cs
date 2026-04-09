using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinancialTracker.Auth.API.Swagger;

public sealed class RequestExamplesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.RequestBody?.Content?.ContainsKey("application/json") != true)
            return;

        var content = operation.RequestBody.Content["application/json"];
        var bodyParam = context.ApiDescription.ParameterDescriptions
            .FirstOrDefault(p => p.Source.Id == "Body");
        var schemaId = content.Schema?.Reference?.Id ?? bodyParam?.Type?.Name;
        var typeName = bodyParam?.Type?.Name ?? schemaId?.Split('.').LastOrDefault() ?? schemaId;

        if (string.IsNullOrEmpty(typeName))
            return;

        var example = GetExampleForSchema(typeName);
        if (example != null)
            content.Example = example;
    }

    private static OpenApiObject? GetExampleForSchema(string schemaId)
    {
        return schemaId switch
        {
            "RegisterRequest" => new OpenApiObject
            {
                ["email"] = new OpenApiString("john.doe@example.com"),
                ["password"] = new OpenApiString("SecurePassword123"),
                ["name"] = new OpenApiString("John Doe")
            },
            "LoginRequest" => new OpenApiObject
            {
                ["email"] = new OpenApiString("john.doe@example.com"),
                ["password"] = new OpenApiString("SecurePassword123")
            },
            _ => null
        };
    }
}
