using FinancialTracker.Auth.Application.DTOs.Request;
using FinancialTracker.Auth.Application.DTOs.Response;
using FinancialTracker.Auth.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace FinancialTracker.Auth.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class AuthController : ControllerBase
{
    private const string ResponseReasonKey = "ResponseLogging.Reason";

    private readonly AuthApplicationService _authService;

    public AuthController(AuthApplicationService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    /// <param name="request">User registration details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created user information</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(request, cancellationToken);
        if (result == null)
        {
            HttpContext.Items[ResponseReasonKey] = "User with this email already exists";
            return Conflict(new { message = "User with this email already exists" });
        }

        HttpContext.Items[ResponseReasonKey] = "User registered successfully";
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>
    /// Login and receive JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User name and JWT token</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken);
        if (result == null)
        {
            HttpContext.Items[ResponseReasonKey] = "Invalid email or password";
            return Unauthorized(new { message = "Invalid email or password" });
        }

        HttpContext.Items[ResponseReasonKey] = "Login successful";
        return Ok(result);
    }
}
