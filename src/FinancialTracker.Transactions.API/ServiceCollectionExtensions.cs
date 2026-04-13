using Confluent.Kafka;
using FinancialTracker.Transactions.API.Clients;
using FinancialTracker.Transactions.API.Outbox;
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
        services.AddHttpClient<IAccountsReadClient, AccountsApiReadClient>(client =>
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

        // Kafka producer (singleton — thread-safe, meant to be reused)
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        services.AddSingleton<IProducer<string, string>>(sp =>
        {
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaBootstrap,
                Acks = Acks.All,
                EnableIdempotence = true
            };
            return new ProducerBuilder<string, string>(config).Build();
        });

        // Outbox relay background service
        services.AddHostedService<OutboxRelayService>();

        return services;
    }
}
