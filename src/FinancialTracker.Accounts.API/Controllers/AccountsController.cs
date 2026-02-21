using System.Security.Claims;
using FinancialTracker.Accounts.Application.DTOs.Request;
using FinancialTracker.Accounts.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace FinancialTracker.Accounts.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/accounts")]
[ApiVersion("1.0")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private const string ResponseReasonKey = "ResponseLogging.Reason";

    private readonly AccountsApplicationService _accountsService;

    public AccountsController(AccountsApplicationService accountsService)
    {
        _accountsService = accountsService;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Create a new finance account
    /// </summary>
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountsService.CreateAccountAsync(UserId, request, cancellationToken);
        HttpContext.Items[ResponseReasonKey] = "Account created successfully";
        return CreatedAtAction(nameof(GetBalance), new { accountId = result!.AccountId }, result);
    }

    /// <summary>
    /// Get all accounts for the authenticated user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialTracker.Accounts.Application.DTOs.Response.BalanceResponse>), 200)]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        var accounts = await _accountsService.GetAccountsAsync(UserId, cancellationToken);
        HttpContext.Items[ResponseReasonKey] = $"Returned {accounts.Count} account(s)";
        return Ok(accounts);
    }

    /// <summary>
    /// Get balance for a specific account
    /// </summary>
    [HttpGet("{accountId:guid}")]
    [ProducesResponseType(typeof(FinancialTracker.Accounts.Application.DTOs.Response.BalanceResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBalance(Guid accountId, CancellationToken cancellationToken)
    {
        var result = await _accountsService.GetBalanceAsync(UserId, accountId, cancellationToken);
        if (result == null)
        {
            HttpContext.Items[ResponseReasonKey] = "Account not found or access denied";
            return NotFound(new { message = "Account not found or access denied" });
        }
        HttpContext.Items[ResponseReasonKey] = "Balance retrieved successfully";
        return Ok(result);
    }
}
