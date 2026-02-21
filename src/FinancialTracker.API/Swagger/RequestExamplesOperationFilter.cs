using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace FinancialTracker.API.Swagger;

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
            "CreateAccountRequest" => new OpenApiObject
            {
                ["name"] = new OpenApiString("Main Wallet"),
                ["currency"] = new OpenApiString("USD")
            },
            "AddIncomeRequest" => new OpenApiObject
            {
                ["accountId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                ["amount"] = new OpenApiDouble(150.50),
                ["category"] = new OpenApiString("Salary"),
                ["note"] = new OpenApiString("Monthly salary")
            },
            "AddExpenseRequest" => new OpenApiObject
            {
                ["accountId"] = new OpenApiString("00000000-0000-0000-0000-000000000001"),
                ["amount"] = new OpenApiDouble(29.99),
                ["category"] = new OpenApiString("Groceries"),
                ["note"] = new OpenApiString("Weekly shopping")
            },
            _ => null
        };
    }
}
