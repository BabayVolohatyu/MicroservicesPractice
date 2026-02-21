using FinancialTracker.Auth.Application.Contracts;
using FinancialTracker.Auth.Application.DTOs.Request;
using FinancialTracker.Auth.Application.DTOs.Response;
using FinancialTracker.Auth.Domain;
using Microsoft.Extensions.Logging;

namespace FinancialTracker.Auth.Application.Services;

public sealed class AuthApplicationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthApplicationService> _logger;

    public AuthApplicationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthApplicationService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<RegisterResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            return null;

        var hash = _passwordHasher.Hash(request.Password);
        var user = User.Create(request.Email, hash, request.Name);
        await _userRepository.AddAsync(user, cancellationToken);

        return new RegisterResponse
        {
            UserId = user.Id,
            Email = user.Email,
            Name = user.Name
        };
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
            return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        var token = _jwtTokenGenerator.Generate(user.Id, user.Email);
        return new LoginResponse
        {
            Name = user.Name,
            Token = "Bearer " + token
        };
    }
}
