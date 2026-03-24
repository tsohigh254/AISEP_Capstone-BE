using AISEP.Application.DTOs.Startup;
using AISEP.Domain.Enums;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateStartupRequestValidator : AbstractValidator<CreateStartupRequest>
{
    // Allowed stages derived from StartupStage enum
    private static readonly string[] AllowedStages =
        Enum.GetNames<StartupStage>();

    public CreateStartupRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required")
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters");

        RuleFor(x => x.IndustryID)
            .GreaterThan(0).When(x => x.IndustryID.HasValue)
            .WithMessage("IndustryID must be a positive integer");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL")
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters");

        RuleFor(x => x.FundingAmountSought)
            .GreaterThanOrEqualTo(0).When(x => x.FundingAmountSought.HasValue)
            .WithMessage("Funding amount sought must be non-negative");

        RuleFor(x => x.CurrentFundingRaised)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentFundingRaised.HasValue)
            .WithMessage("Current funding raised must be non-negative");

        RuleFor(x => x.Valuation)
            .GreaterThanOrEqualTo(0).When(x => x.Valuation.HasValue)
            .WithMessage("Valuation must be non-negative");

    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateStartupRequestValidator : AbstractValidator<UpdateStartupRequest>
{
    private static readonly string[] AllowedStages =
        Enum.GetNames<StartupStage>();

    public UpdateStartupRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Company name must not exceed 200 characters")
            .NotEmpty().When(x => x.CompanyName != null).WithMessage("Company name cannot be empty");

        RuleFor(x => x.IndustryID)
            .GreaterThan(0).When(x => x.IndustryID.HasValue)
            .WithMessage("IndustryID must be a positive integer");


        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Description must not exceed 5000 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL")
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters");


        RuleFor(x => x.FundingAmountSought)
            .GreaterThanOrEqualTo(0).When(x => x.FundingAmountSought.HasValue)
            .WithMessage("Funding amount sought must be non-negative");

        RuleFor(x => x.CurrentFundingRaised)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentFundingRaised.HasValue)
            .WithMessage("Current funding raised must be non-negative");

        RuleFor(x => x.Valuation)
            .GreaterThanOrEqualTo(0).When(x => x.Valuation.HasValue)
            .WithMessage("Valuation must be non-negative");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateTeamMemberRequestValidator : AbstractValidator<CreateTeamMemberRequest>
{
    public CreateTeamMemberRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required")
            .MaximumLength(100).WithMessage("Role must not exceed 100 characters");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL")
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue)
            .WithMessage("Years of experience must be non-negative");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateTeamMemberRequestValidator : AbstractValidator<UpdateTeamMemberRequest>
{
    public UpdateTeamMemberRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters")
            .NotEmpty().When(x => x.FullName != null).WithMessage("Full name cannot be empty");

        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage("Role must not exceed 100 characters");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL")
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue)
            .WithMessage("Years of experience must be non-negative");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
