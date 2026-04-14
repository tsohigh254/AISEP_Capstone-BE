using AISEP.Application.DTOs.Connection;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateConnectionRequestValidator : AbstractValidator<CreateConnectionRequest>
{
    public CreateConnectionRequestValidator()
    {
        RuleFor(x => x.StartupId)
            .GreaterThan(0).WithMessage("StartupId must be a positive integer.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Lời nhắn không được để trống.")
            .MaximumLength(300).WithMessage("Lời nhắn tối đa 300 ký tự.");
    }
}

public class UpdateConnectionRequestValidator : AbstractValidator<UpdateConnectionRequest>
{
    public UpdateConnectionRequestValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(2000).WithMessage("Message must not exceed 2000 characters.")
            .When(x => x.Message != null);
    }
}

public class RejectConnectionRequestValidator : AbstractValidator<RejectConnectionRequest>
{
    public RejectConnectionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.")
            .When(x => x.Reason != null);
    }
}

public class CreateInfoRequestValidator : AbstractValidator<CreateInfoRequest>
{
    public CreateInfoRequestValidator()
    {
        RuleFor(x => x.RequestType)
            .NotEmpty().WithMessage("Request type is required.")
            .MaximumLength(200).WithMessage("Request type must not exceed 200 characters.");

        RuleFor(x => x.RequestMessage)
            .MaximumLength(2000).WithMessage("Request message must not exceed 2000 characters.")
            .When(x => x.RequestMessage != null);
    }
}

public class FulfillInfoRequestValidator : AbstractValidator<FulfillInfoRequest>
{
    public FulfillInfoRequestValidator()
    {
        RuleFor(x => x.ResponseMessage)
            .MaximumLength(4000).WithMessage("Response message must not exceed 4000 characters.")
            .When(x => x.ResponseMessage != null);
    }
}

public class CreatePortfolioCompanyRequestValidator : AbstractValidator<CreatePortfolioCompanyRequest>
{
    public CreatePortfolioCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.");

        RuleFor(x => x.Industry)
            .MaximumLength(200).WithMessage("Industry must not exceed 200 characters.")
            .When(x => x.Industry != null);

        RuleFor(x => x.InvestmentStage)
            .MaximumLength(100).WithMessage("Investment stage must not exceed 100 characters.")
            .When(x => x.InvestmentStage != null);

        RuleFor(x => x.InvestmentAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Investment amount must be >= 0.")
            .When(x => x.InvestmentAmount.HasValue);

        RuleFor(x => x.CurrentStatus)
            .MaximumLength(100).WithMessage("Status must not exceed 100 characters.")
            .When(x => x.CurrentStatus != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);
    }
}

public class UpdatePortfolioCompanyRequestValidator : AbstractValidator<UpdatePortfolioCompanyRequest>
{
    public UpdatePortfolioCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters.")
            .When(x => x.CompanyName != null);

        RuleFor(x => x.Industry)
            .MaximumLength(200).WithMessage("Industry must not exceed 200 characters.")
            .When(x => x.Industry != null);

        RuleFor(x => x.InvestmentStage)
            .MaximumLength(100).WithMessage("Investment stage must not exceed 100 characters.")
            .When(x => x.InvestmentStage != null);

        RuleFor(x => x.InvestmentAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Investment amount must be >= 0.")
            .When(x => x.InvestmentAmount.HasValue);

        RuleFor(x => x.CurrentStatus)
            .MaximumLength(100).WithMessage("Status must not exceed 100 characters.")
            .When(x => x.CurrentStatus != null);

        RuleFor(x => x.ExitType)
            .MaximumLength(100).WithMessage("Exit type must not exceed 100 characters.")
            .When(x => x.ExitType != null);

        RuleFor(x => x.ExitValue)
            .GreaterThanOrEqualTo(0).WithMessage("Exit value must be >= 0.")
            .When(x => x.ExitValue.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);
    }
}
