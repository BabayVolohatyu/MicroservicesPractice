namespace FinancialTracker.Auth.Application.DTOs.Response;

public sealed class LoginResponse
{
    public string Name { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}
