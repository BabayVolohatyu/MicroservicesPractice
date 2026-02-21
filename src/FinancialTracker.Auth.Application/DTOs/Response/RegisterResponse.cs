namespace FinancialTracker.Auth.Application.DTOs.Response;

public sealed class RegisterResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
