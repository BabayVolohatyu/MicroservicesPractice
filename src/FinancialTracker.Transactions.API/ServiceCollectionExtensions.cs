using FinancialTracker.Transactions.API.Clients;
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
using Polly;
using Polly.Extensions.Http;

namespace FinancialTracker.Transactions.API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AccountsClientOptions>(configuration.GetSection(AccountsClientOptions.SectionName));
        var accountsOptions = configuration.GetSection(AccountsClientOptions.SectionName).Get<AccountsClientOptions>() ?? new AccountsClientOptions();

        services.AddScoped<TransactionsApplicationService>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddHttpClient<IAccountBalanceGateway, AccountsBalanceGatewayHttpClient>(client =>
        {
            client.BaseAddress = new Uri(accountsOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(accountsOptions.TimeoutSeconds);
        })
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(response => (int)response.StatusCode == 429)
            .WaitAndRetryAsync(
                accountsOptions.RetryCount,
                retryAttempt => TimeSpan.FromMilliseconds(accountsOptions.RetryBaseDelayMs * retryAttempt)))
        .AddPolicyHandler(HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                accountsOptions.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(accountsOptions.CircuitBreakerBreakSeconds)));
        services.AddValidatorsFromAssemblyContaining<AddIncomeRequestValidator>();
        services.AddHttpContextAccessor();

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
