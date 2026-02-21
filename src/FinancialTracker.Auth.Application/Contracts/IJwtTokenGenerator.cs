namespace FinancialTracker.Auth.Application.Contracts;

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email);
}
