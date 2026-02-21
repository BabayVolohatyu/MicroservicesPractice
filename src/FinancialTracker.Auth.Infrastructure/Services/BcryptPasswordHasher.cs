using FinancialTracker.Auth.Application.Contracts;

namespace FinancialTracker.Auth.Infrastructure.Services;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
}
