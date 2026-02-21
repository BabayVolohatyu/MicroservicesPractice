using FinancialTracker.Transactions.Application.DTOs.Request;
using FluentValidation;

namespace FinancialTracker.Transactions.Application.Validators;

public sealed class AddIncomeRequestValidator : AbstractValidator<AddIncomeRequest>
{
    public AddIncomeRequestValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty().WithMessage("AccountId is required");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive");
        RuleFor(x => x.Category).MaximumLength(100).When(x => x.Category != null);
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
    }
}
