using FinancialTracker.Accounts.Application.Contracts;
using FinancialTracker.Accounts.Application.DTOs.Request;
using FinancialTracker.Accounts.Application.DTOs.Response;
using FinancialTracker.Accounts.Domain;

namespace FinancialTracker.Accounts.Application.Services;

public sealed class AccountsApplicationService : IAccountBalanceUpdater
{
    private readonly IFinanceAccountRepository _accountRepository;

    public AccountsApplicationService(IFinanceAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<CreateAccountResponse?> CreateAccountAsync(Guid userId, CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        var account = FinanceAccount.Create(userId, request.Name, request.Currency);
        await _accountRepository.AddAsync(account, cancellationToken);
        return new CreateAccountResponse
        {
            AccountId = account.Id,
            Name = account.Name,
            Currency = account.Currency
        };
    }

    public async Task<BalanceResponse?> GetBalanceAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.OwnerId != userId)
            return null;
        return new BalanceResponse
        {
            AccountId = account.Id,
            Name = account.Name,
            Balance = account.Balance,
            Currency = account.Currency
        };
    }

    public async Task<IReadOnlyList<BalanceResponse>> GetAccountsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var accounts = await _accountRepository.GetByOwnerIdAsync(userId, cancellationToken);
        return accounts.Select(a => new BalanceResponse
        {
            AccountId = a.Id,
            Name = a.Name,
            Balance = a.Balance,
            Currency = a.Currency
        }).ToList();
    }

    public async Task<bool> AddBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.OwnerId != userId) return false;
        account.Credit(amount);
        await _accountRepository.UpdateAsync(account, cancellationToken);
        return true;
    }

    public async Task<SubtractBalanceResult> SubtractBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetByIdAsync(accountId, cancellationToken);
        if (account == null || account.OwnerId != userId)
            return SubtractBalanceResult.AccountNotFound();
        try
        {
            account.Debit(amount);
        }
        catch (InvalidOperationException)
        {
            return SubtractBalanceResult.InsufficientBalance();
        }
        await _accountRepository.UpdateAsync(account, cancellationToken);
        return SubtractBalanceResult.Succeeded();
    }
}
