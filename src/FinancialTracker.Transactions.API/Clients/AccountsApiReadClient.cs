using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinancialTracker.Transactions.Application.Contracts;
using Polly.CircuitBreaker;

namespace FinancialTracker.Transactions.API.Clients;

public sealed class AccountsApiReadClient : IAccountsReadClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccountsApiReadClient> _logger;

    public AccountsApiReadClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AccountsApiReadClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AccountsBalanceReadResult> GetBalanceAsync(Guid userId, Guid accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/accounts/{accountId:D}");
            CopyHeaders(request.Headers);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var body = await response.Content.ReadFromJsonAsync<BalanceResponseDto>(cancellationToken);
                if (body is null)
                    return new AccountsBalanceReadResult(AccountsReadStatus.Unavailable, 0);
                return new AccountsBalanceReadResult(AccountsReadStatus.Ok, body.Balance);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return new AccountsBalanceReadResult(AccountsReadStatus.NotFound, 0);

            return new AccountsBalanceReadResult(AccountsReadStatus.Unavailable, 0);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Accounts service circuit breaker is open");
            return new AccountsBalanceReadResult(AccountsReadStatus.Unavailable, 0);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Accounts service request failed");
            return new AccountsBalanceReadResult(AccountsReadStatus.Unavailable, 0);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Accounts service request timed out or was canceled");
            return new AccountsBalanceReadResult(AccountsReadStatus.Unavailable, 0);
        }
    }

    private void CopyHeaders(HttpRequestHeaders headers)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorization) && !string.IsNullOrWhiteSpace(authorization))
            headers.TryAddWithoutValidation("Authorization", authorization.ToString());

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && !string.IsNullOrWhiteSpace(correlationId))
            headers.TryAddWithoutValidation("X-Correlation-ID", correlationId.ToString());
    }

    private sealed record BalanceResponseDto(Guid AccountId, string Name, decimal Balance, string Currency);
}
