using FinancialTracker.Accounts.API.Persistence;
using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Application.Services;
using FinancialTracker.Accounts.Application.Validators;
using FinancialTracker.Accounts.Infrastructure.Persistence;
using FinancialTracker.Accounts.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTracker.Accounts.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccountsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AccountsApplicationService>();
        services.AddScoped<IFinanceAccountRepository, FinanceAccountRepository>();
        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();

        // Single shared in-memory SQLite connection so EnsureCreated and all requests see the same DB
        services.AddSingleton<AccountsDbConnectionHolder>();
        services.AddDbContext<AccountsDbContext>((sp, ob) =>
        {
            var holder = sp.GetRequiredService<AccountsDbConnectionHolder>();
            ob.UseSqlite(holder.Connection);
        });

        return services;
    }
}
