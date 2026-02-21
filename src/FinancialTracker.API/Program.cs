using FinancialTracker.API.HealthChecks;
using FinancialTracker.API.Middleware;
using FinancialTracker.API.Services;
using FinancialTracker.API.Swagger;
using FinancialTracker.Auth.API;
using FinancialTracker.Auth.API.Persistence;
using FinancialTracker.Accounts.API;
using FinancialTracker.Accounts.API.Persistence;
using FinancialTracker.Transactions.API;
using FinancialTracker.Transactions.API.Persistence;
using FinancialTracker.Auth.Infrastructure.Persistence;
using FinancialTracker.Auth.Infrastructure.Services;
using FinancialTracker.Accounts.Infrastructure.Persistence;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add API versioning
builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader()
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// FluentValidation automatic validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<FinancialTracker.Auth.Application.Validators.RegisterRequestValidator>(ServiceLifetime.Scoped);
builder.Services.AddValidatorsFromAssemblyContaining<FinancialTracker.Accounts.Application.Validators.CreateAccountRequestValidator>(ServiceLifetime.Scoped);
builder.Services.AddValidatorsFromAssemblyContaining<FinancialTracker.Transactions.Application.Validators.AddIncomeRequestValidator>(ServiceLifetime.Scoped);

// Enhanced Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Financial Tracker API",
        Version = "v1",
        Description = "A modular monolith financial tracker API with authentication, accounts, and transactions management.",
        Contact = new OpenApiContact
        {
            Name = "Financial Tracker",
            Email = "support@financialtracker.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add valid example requests for every endpoint
    c.OperationFilter<RequestExamplesOperationFilter>();
});

// JWT Configuration from environment variables (with fallback to appsettings.json)
// Environment variables take precedence over config file values
var jwtSecret = builder.Configuration["JWT_SECRET"] 
    ?? throw new InvalidOperationException("JWT_SECRET must be set in environment variables or appsettings.json");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] 
    ?? throw new InvalidOperationException("JWT_ISSUER must be set in environment variables or appsettings.json");
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] 
    ?? throw new InvalidOperationException("JWT_AUDIENCE must be set in environment variables or appsettings.json");

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// Health checks: all IHealthCheck types in this assembly are registered by convention.
// To add a new check, create a class implementing IHealthCheck (optionally with [HealthCheckRegistration]).
builder.Services.AddHealthChecks()
    .AddAllHealthChecksFromAssembly(typeof(Program).Assembly);

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddAccountsModule(builder.Configuration);
builder.Services.AddTransactionsModule(builder.Configuration);

// Note: DatabaseConnectionKeeper will be created after app.Build() to access scoped services

var app = builder.Build();

// Response logging (wraps pipeline to log status code and reason for every response)
app.UseMiddleware<ResponseLoggingMiddleware>();
// Exception handling middleware
app.UseMiddleware<ValidationExceptionHandlerMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// Initialize databases and keep connections open for in-memory SQLite
// Create a scope that lives for the app lifetime
var serviceScope = app.Services.CreateScope();
var connectionKeeper = new DatabaseConnectionKeeper(
    serviceScope.ServiceProvider.GetRequiredService<AuthDbContext>(),
    serviceScope.ServiceProvider.GetRequiredService<AccountsDbContext>(),
    serviceScope.ServiceProvider.GetRequiredService<TransactionsDbContext>());

// Dispose everything when app stops
app.Lifetime.ApplicationStopped.Register(() =>
{
    connectionKeeper?.Dispose();
    serviceScope?.ServiceProvider.GetService<AuthDbConnectionHolder>()?.Dispose();
    serviceScope?.ServiceProvider.GetService<AccountsDbConnectionHolder>()?.Dispose();
    serviceScope?.ServiceProvider.GetService<TransactionsDbConnectionHolder>()?.Dispose();
    serviceScope?.Dispose();
});

// Static files (e.g. for Swagger UI custom script)
app.UseStaticFiles();

// Enable Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Financial Tracker API v1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger
        c.DisplayRequestDuration();
        c.InjectJavascript("/swagger-copy-to-clipboard.js");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriters.WriteJsonAsync
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriters.WriteJsonAsync
});
app.MapHealthChecks("/status", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthCheckResponseWriters.WriteJsonAsync
});

app.MapControllers();

app.Run();
