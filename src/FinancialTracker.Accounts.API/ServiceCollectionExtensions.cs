using Confluent.Kafka;
using FinancialTracker.Accounts.API.Kafka;
using FinancialTracker.Accounts.API.Persistence;
using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Application.Services;
using FinancialTracker.Accounts.Application.Validators;
using FinancialTracker.Accounts.Infrastructure.Persistence;
using FinancialTracker.Accounts.Infrastructure.Repositories;
using FinancialTracker.Accounts.Infrastructure.Services;
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
        services.AddScoped<ILedgerTransactionApplier, LedgerTransactionApplier>();
        services.AddValidatorsFromAssemblyContaining<CreateAccountRequestValidator>();

        // Single shared in-memory SQLite connection so EnsureCreated and all requests see the same DB
        services.AddSingleton<AccountsDbConnectionHolder>();
        services.AddDbContext<AccountsDbContext>((sp, ob) =>
        {
            var holder = sp.GetRequiredService<AccountsDbConnectionHolder>();
            ob.UseSqlite(holder.Connection);
        });

        // Kafka consumer (singleton — one consumer per consumer group)
        var kafkaBootstrap = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        var kafkaGroupId = configuration["Kafka:GroupId"] ?? "accounts-service";
        services.AddSingleton<IConsumer<string, string>>(sp =>
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = kafkaBootstrap,
                GroupId = kafkaGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                LogConnectionClose = false,
                ReconnectBackoffMs = 1000,
                ReconnectBackoffMaxMs = 10000,
            };
            var logger = sp.GetRequiredService<ILogger<TransactionEventConsumer>>();
            return new ConsumerBuilder<string, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    if (e.IsFatal)
                        logger.LogError("Kafka fatal error: {Reason}", e.Reason);
                    else
                        logger.LogDebug("Kafka error: {Reason}", e.Reason);
                })
                .Build();
        });

        // Kafka consumer background service
        services.AddHostedService<TransactionEventConsumer>();

        return services;
    }
}
