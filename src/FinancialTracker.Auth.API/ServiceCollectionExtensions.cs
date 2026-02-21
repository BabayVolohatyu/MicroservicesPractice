using FinancialTracker.Auth.API.Persistence;
using FinancialTracker.Auth.Application.Contracts;
using FinancialTracker.Auth.Application.Services;
using FinancialTracker.Auth.Application.Validators;
using FinancialTracker.Auth.Infrastructure.Persistence;
using FinancialTracker.Auth.Infrastructure.Repositories;
using FinancialTracker.Auth.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTracker.Auth.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuthApplicationService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        
        // JWT Options from environment variables (with fallback to appsettings.json)
        var jwtOptions = new JwtOptions
        {
            Secret = configuration["JWT_SECRET"] ?? throw new InvalidOperationException("JWT_SECRET must be set in environment variables or appsettings.json"),
            Issuer = configuration["JWT_ISSUER"] ?? throw new InvalidOperationException("JWT_ISSUER must be set in environment variables or appsettings.json"),
            Audience = configuration["JWT_AUDIENCE"] ?? throw new InvalidOperationException("JWT_AUDIENCE must be set in environment variables or appsettings.json"),
            ExpirationHours = int.TryParse(configuration["JWT_EXPIRATION_HOURS"], out var hours) ? hours : 24
        };
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(jwtOptions));
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        // Single shared in-memory SQLite connection so EnsureCreated and all requests see the same DB
        services.AddSingleton<AuthDbConnectionHolder>();
        services.AddDbContext<AuthDbContext>((sp, ob) =>
        {
            var holder = sp.GetRequiredService<AuthDbConnectionHolder>();
            ob.UseSqlite(holder.Connection);
        });

        return services;
    }
}
