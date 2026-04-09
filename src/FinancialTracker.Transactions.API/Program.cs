using System.Text;
using FinancialTracker.Transactions.API;
using FinancialTracker.Transactions.API.Middleware;
using FinancialTracker.Transactions.API.Persistence;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = true;
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
});
builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Transactions API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Authorization: Bearer {token}"
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
});

var jwtSecret = builder.Configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET must be set in environment variables");
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? throw new InvalidOperationException("JWT_ISSUER must be set in environment variables");
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? throw new InvalidOperationException("JWT_AUDIENCE must be set in environment variables");
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
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

builder.Services.AddTransactionsModule(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
    db.Database.EnsureCreated();
}

app.Lifetime.ApplicationStopping.Register(() =>
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetService<TransactionsDbConnectionHolder>()?.Dispose();
});

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
