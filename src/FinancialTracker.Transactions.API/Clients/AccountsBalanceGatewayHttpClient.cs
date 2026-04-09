using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinancialTracker.Transactions.Application.Contracts;
using Polly.CircuitBreaker;

namespace FinancialTracker.Transactions.API.Clients;

public sealed class AccountsBalanceGatewayHttpClient : IAccountBalanceGateway
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccountsBalanceGatewayHttpClient> _logger;

    public AccountsBalanceGatewayHttpClient(
        HttpClient httpClient,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AccountsBalanceGatewayHttpClient> logger)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<bool> AddBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var request = new UpdateBalanceRequest(userId, amount);
        using var response = await SendAsync(HttpMethod.Post, $"api/v1/internal/accounts/{accountId:D}/credit", request, cancellationToken);
        return response.StatusCode == HttpStatusCode.OK;
    }

    public async Task<SubtractBalanceResult> SubtractBalanceAsync(Guid accountId, Guid userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var request = new UpdateBalanceRequest(userId, amount);
        using var response = await SendAsync(HttpMethod.Post, $"api/v1/internal/accounts/{accountId:D}/debit", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return SubtractBalanceResult.Succeeded();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return SubtractBalanceResult.AccountNotFound();
        }

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            return SubtractBalanceResult.InsufficientBalance();
        }

        return SubtractBalanceResult.ServiceUnavailable();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object body, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, path)
            {
                Content = JsonContent.Create(body)
            };

            CopyHeaders(request.Headers);

            return await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Accounts service circuit breaker is open");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Accounts service request failed");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Accounts service request timed out or was canceled");
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }
    }

    private void CopyHeaders(HttpRequestHeaders headers)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        if (httpContext.Request.Headers.TryGetValue("Authorization", out var authorization) && !string.IsNullOrWhiteSpace(authorization))
        {
            headers.TryAddWithoutValidation("Authorization", authorization.ToString());
        }

        if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) && !string.IsNullOrWhiteSpace(correlationId))
        {
            headers.TryAddWithoutValidation("X-Correlation-ID", correlationId.ToString());
        }
    }

    private sealed record UpdateBalanceRequest(Guid UserId, decimal Amount);
}
