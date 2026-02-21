using FinancialTracker.Accounts.Application.DTOs.Request;
using FluentValidation;

namespace FinancialTracker.Accounts.Application.Validators;

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Account name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter code");
    }
}
