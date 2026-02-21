namespace FinancialTracker.Auth.Domain;

public sealed class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private User() { }

    public static User Create(string email, string passwordHash, string name)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            Name = name,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}
