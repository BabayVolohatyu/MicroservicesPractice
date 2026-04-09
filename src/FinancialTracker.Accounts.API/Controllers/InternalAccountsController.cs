using FinancialTracker.Accounts.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Accounts.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/internal/accounts")]
[ApiVersion("1.0")]
[Authorize]
public sealed class InternalAccountsController : ControllerBase
{
    private readonly AccountsApplicationService _accountsService;

    public InternalAccountsController(AccountsApplicationService accountsService)
    {
        _accountsService = accountsService;
    }

    [HttpPost("{accountId:guid}/credit")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Credit(Guid accountId, [FromBody] UpdateBalanceRequest request, CancellationToken cancellationToken)
    {
        var updated = await _accountsService.AddBalanceAsync(accountId, request.UserId, request.Amount, cancellationToken);
        return updated ? Ok() : NotFound();
    }

    [HttpPost("{accountId:guid}/debit")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> Debit(Guid accountId, [FromBody] UpdateBalanceRequest request, CancellationToken cancellationToken)
    {
        var result = await _accountsService.SubtractBalanceAsync(accountId, request.UserId, request.Amount, cancellationToken);
        if (result.Success)
        {
            return Ok();
        }

        if (result.FailureReason == FinancialTracker.Accounts.Application.Contracts.SubtractBalanceFailureReason.InsufficientBalance)
        {
            return UnprocessableEntity();
        }

        return NotFound();
    }

    public sealed record UpdateBalanceRequest(Guid UserId, decimal Amount);
}
