using FinancialTracker.Auth.Application.DTOs.Request;
using FluentValidation;

namespace FinancialTracker.Auth.Application.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
