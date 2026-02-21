namespace FinancialTracker.Auth.Application.DTOs.Request;

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
