using AISEP.Application.DTOs.Investor;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class SubmitInvestorKYCRequestValidator : AbstractValidator<SubmitInvestorKYCRequest>
{
    private static readonly string[] AllowedCategories = { "INSTITUTIONAL", "INDIVIDUAL_ANGEL" };

    public SubmitInvestorKYCRequestValidator()
    {
        RuleFor(x => x.InvestorCategory)
            .NotEmpty().WithMessage("InvestorCategory is required.")
            .Must(c => AllowedCategories.Contains(c))
            .WithMessage($"InvestorCategory must be one of: {string.Join(", ", AllowedCategories)}");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.")
            .MaximumLength(200).WithMessage("FullName must not exceed 200 characters");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("ContactEmail is required.")
            .EmailAddress().WithMessage("ContactEmail must be a valid email address");
    }
}

public class SaveInvestorKYCDraftRequestValidator : AbstractValidator<SaveInvestorKYCDraftRequest>
{
    private static readonly string[] AllowedCategories = { "INSTITUTIONAL", "INDIVIDUAL_ANGEL" };

    public SaveInvestorKYCDraftRequestValidator()
    {
        RuleFor(x => x.InvestorCategory)
            .Must(c => c == null || AllowedCategories.Contains(c))
            .WithMessage($"InvestorCategory must be one of: {string.Join(", ", AllowedCategories)}");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("ContactEmail must be a valid email address")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class CreateInvestorRequestValidator : AbstractValidator<CreateInvestorRequest>
{
    public CreateInvestorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required")
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.FirmName)
            .MaximumLength(200).WithMessage("Firm name must not exceed 200 characters");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(5000).WithMessage("Bio must not exceed 5000 characters");

        RuleFor(x => x.InvestmentThesis)
            .MaximumLength(5000).WithMessage("Investment thesis must not exceed 5000 characters");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL")
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL")
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateInvestorRequestValidator : AbstractValidator<UpdateInvestorRequest>
{
    public UpdateInvestorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Full name must not exceed 200 characters");

        RuleFor(x => x.FirmName)
            .MaximumLength(200).WithMessage("Firm name must not exceed 200 characters");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Bio)
            .MaximumLength(5000).WithMessage("Bio must not exceed 5000 characters");

        RuleFor(x => x.InvestmentThesis)
            .MaximumLength(5000).WithMessage("Investment thesis must not exceed 5000 characters");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL")
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL")
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    private static readonly string[] AllowedStages =
    {
        "Idea", "PreSeed", "Seed", "SeriesA", "SeriesB", "SeriesC", "Growth"
    };

    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.TicketMin)
            .GreaterThanOrEqualTo(0).When(x => x.TicketMin.HasValue)
            .WithMessage("Minimum ticket size must be non-negative");

        RuleFor(x => x.TicketMax)
            .GreaterThanOrEqualTo(0).When(x => x.TicketMax.HasValue)
            .WithMessage("Maximum ticket size must be non-negative");

        RuleFor(x => x)
            .Must(x => !x.TicketMin.HasValue || !x.TicketMax.HasValue || x.TicketMin <= x.TicketMax)
            .WithMessage("ticketMin must be less than or equal to ticketMax")
            .WithName("TicketRange");

        RuleForEach(x => x.PreferredStages)
            .Must(s => AllowedStages.Contains(s))
            .WithMessage(s => $"Stage '{{PropertyValue}}' is not valid. Allowed: {string.Join(", ", AllowedStages)}");

        RuleFor(x => x.PreferredGeographies)
            .MaximumLength(1000).WithMessage("Preferred geographies must not exceed 1000 characters");

        RuleFor(x => x.MinPotentialScore)
            .InclusiveBetween(0f, 100f).When(x => x.MinPotentialScore.HasValue)
            .WithMessage("Min potential score must be between 0 and 100");
    }
}

public class WatchlistAddRequestValidator : AbstractValidator<WatchlistAddRequest>
{
    private static readonly string[] AllowedPriorities = { "Low", "Medium", "High" };

    public WatchlistAddRequestValidator()
    {
        RuleFor(x => x.StartupId)
            .GreaterThan(0).WithMessage("startupId must be greater than 0");

        RuleFor(x => x.WatchReason)
            .MaximumLength(1000).WithMessage("Watch reason must not exceed 1000 characters");

        RuleFor(x => x.Priority)
            .Must(p => string.IsNullOrEmpty(p) || AllowedPriorities.Contains(p))
            .WithMessage($"Priority must be one of: {string.Join(", ", AllowedPriorities)}");
    }
}
