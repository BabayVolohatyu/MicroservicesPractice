using System.Security.Claims;
using FinancialTracker.Transactions.Application.DTOs.Request;
using FinancialTracker.Transactions.Application.DTOs.Response;
using FinancialTracker.Transactions.Application.Contracts;
using FinancialTracker.Transactions.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace FinancialTracker.Transactions.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/transactions")]
[ApiVersion("1.0")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
    private const string ResponseReasonKey = "ResponseLogging.Reason";

    private readonly TransactionsApplicationService _transactionsService;

    public TransactionsController(TransactionsApplicationService transactionsService)
    {
        _transactionsService = transactionsService;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException());

    /// <summary>
    /// Add an income transaction to an account
    /// </summary>
    [HttpPost("income")]
    [ProducesResponseType(typeof(TransactionResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AddIncome([FromBody] AddIncomeRequest request, CancellationToken cancellationToken)
    {
        var result = await _transactionsService.AddIncomeAsync(UserId, request, cancellationToken);
        if (result == null)
        {
            HttpContext.Items[ResponseReasonKey] = "Account not found or access denied";
            return NotFound(new { message = "Account not found or access denied" });
        }
        HttpContext.Items[ResponseReasonKey] = "Income added successfully";
        return CreatedAtAction(nameof(AddIncome), result);
    }

    /// <summary>
    /// Add an expense transaction to an account
    /// </summary>
    [HttpPost("expense")]
    [ProducesResponseType(typeof(TransactionResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(422)]
    public async Task<IActionResult> AddExpense([FromBody] AddExpenseRequest request, CancellationToken cancellationToken)
    {
        var result = await _transactionsService.AddExpenseAsync(UserId, request, cancellationToken);
        if (!result.Success)
        {
            if (result.FailureReason == SubtractBalanceFailureReason.ServiceUnavailable)
            {
                HttpContext.Items[ResponseReasonKey] = "Accounts service unavailable";
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Accounts service temporarily unavailable" });
            }

            if (result.FailureReason == SubtractBalanceFailureReason.InsufficientBalance)
            {
                HttpContext.Items[ResponseReasonKey] = "Insufficient balance";
                return UnprocessableEntity(new { message = "Insufficient balance" });
            }
            HttpContext.Items[ResponseReasonKey] = "Account not found or access denied";
            return NotFound(new { message = "Account not found or access denied" });
        }
        HttpContext.Items[ResponseReasonKey] = "Expense added successfully";
        return CreatedAtAction(nameof(AddExpense), result.Data);
    }
}
