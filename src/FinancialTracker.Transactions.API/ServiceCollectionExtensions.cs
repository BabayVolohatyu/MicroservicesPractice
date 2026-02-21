using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Application.Services;
using FinancialTracker.Transactions.API.Persistence;
using FinancialTracker.Transactions.Application.Contracts;
using FinancialTracker.Transactions.Application.Services;
using FinancialTracker.Transactions.Application.Validators;
using FinancialTracker.Transactions.Infrastructure.Persistence;
using FinancialTracker.Transactions.Infrastructure.Repositories;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinancialTracker.Transactions.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<TransactionsApplicationService>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IAccountBalanceUpdater>(sp => sp.GetRequiredService<AccountsApplicationService>());
        services.AddValidatorsFromAssemblyContaining<AddIncomeRequestValidator>();

        // Single shared in-memory SQLite connection so EnsureCreated and all requests see the same DB
        services.AddSingleton<TransactionsDbConnectionHolder>();
        services.AddDbContext<TransactionsDbContext>((sp, ob) =>
        {
            var holder = sp.GetRequiredService<TransactionsDbConnectionHolder>();
            ob.UseSqlite(holder.Connection);
        });

        return services;
    }
}
