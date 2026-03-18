using AISEP.Application.DTOs.Advisor;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateAdvisorRequestValidator : AbstractValidator<CreateAdvisorRequest>
{
    public CreateAdvisorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.");

        RuleFor(x => x.Title)
            .MaximumLength(120).WithMessage("Title must not exceed 120 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Company)
            .MaximumLength(200).WithMessage("Company must not exceed 200 characters.")
            .When(x => x.Company != null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => x.Bio != null);

        RuleFor(x => x.Website)
            .Must(AdvisorUrlValidationHelper.BeAValidUrlOrDomainOrNull).WithMessage("Website must be a valid URL.")
            .When(x => !AdvisorUrlValidationHelper.IsNullOrPlaceholder(x.Website));

        RuleFor(x => x.LinkedInURL)
            .Must(AdvisorUrlValidationHelper.BeAValidUrlOrDomainOrNull).WithMessage("LinkedIn URL must be a valid URL.")
            .When(x => !AdvisorUrlValidationHelper.IsNullOrPlaceholder(x.LinkedInURL));

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Mentorship philosophy must not exceed 2000 characters.")
            .When(x => x.MentorshipPhilosophy != null);
    }
}

public class UpdateAdvisorRequestValidator : AbstractValidator<UpdateAdvisorRequest>
{
    public UpdateAdvisorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(120).WithMessage("Full name must not exceed 120 characters.")
            .When(x => x.FullName != null);

        RuleFor(x => x.Title)
            .MaximumLength(120).WithMessage("Title must not exceed 120 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Company)
            .MaximumLength(200).WithMessage("Company must not exceed 200 characters.")
            .When(x => x.Company != null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Bio must not exceed 2000 characters.")
            .When(x => x.Bio != null);

        RuleFor(x => x.Website)
            .Must(AdvisorUrlValidationHelper.BeAValidUrlOrDomainOrNull).WithMessage("Website must be a valid URL.")
            .When(x => !AdvisorUrlValidationHelper.IsNullOrPlaceholder(x.Website));

        RuleFor(x => x.LinkedInURL)
            .Must(AdvisorUrlValidationHelper.BeAValidUrlOrDomainOrNull).WithMessage("LinkedIn URL must be a valid URL.")
            .When(x => !AdvisorUrlValidationHelper.IsNullOrPlaceholder(x.LinkedInURL));

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Mentorship philosophy must not exceed 2000 characters.")
            .When(x => x.MentorshipPhilosophy != null);
    }
}

internal static class AdvisorUrlValidationHelper
{
    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "k co", "khong co", "khong", "none", "n/a", "na", "null"
    };

    public static bool IsNullOrPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        return PlaceholderValues.Contains(value.Trim());
    }

    public static bool BeAValidUrlOrDomainOrNull(string? value)
    {
        if (IsNullOrPlaceholder(value))
            return true;

        var normalized = Normalize(value!);
        return normalized is not null;
    }

    public static string? Normalize(string value)
    {
        var input = value.Trim();
        if (string.IsNullOrWhiteSpace(input) || PlaceholderValues.Contains(input))
            return null;

        if (!input.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !input.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            input = $"https://{input}";
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return null;

        if (string.IsNullOrWhiteSpace(uri.Host) || !uri.Host.Contains('.'))
            return null;

        return uri.ToString();
    }
}

public class UpdateExpertiseRequestValidator : AbstractValidator<UpdateExpertiseRequest>
{
    public UpdateExpertiseRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotNull().WithMessage("Items list is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(e => e.Category)
                .NotEmpty().WithMessage("Expertise category is required.")
                .MaximumLength(100).WithMessage("Category must not exceed 100 characters.");

            item.RuleFor(e => e.SubTopic)
                .MaximumLength(200).WithMessage("SubTopic must not exceed 200 characters.")
                .When(e => e.SubTopic != null);

            item.RuleFor(e => e.YearsOfExperience)
                .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be >= 0.")
                .When(e => e.YearsOfExperience.HasValue);
        });
    }
}

public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.SessionFormats)
            .MaximumLength(500).WithMessage("Session formats must not exceed 500 characters.")
            .When(x => x.SessionFormats != null);

        RuleFor(x => x.TypicalSessionDuration)
            .InclusiveBetween(15, 480).WithMessage("Typical session duration must be between 15 and 480 minutes.")
            .When(x => x.TypicalSessionDuration.HasValue);

        RuleFor(x => x.WeeklyAvailableHours)
            .InclusiveBetween(1, 168).WithMessage("Weekly available hours must be between 1 and 168.")
            .When(x => x.WeeklyAvailableHours.HasValue);

        RuleFor(x => x.MaxConcurrentMentees)
            .InclusiveBetween(1, 100).WithMessage("Max concurrent mentees must be between 1 and 100.")
            .When(x => x.MaxConcurrentMentees.HasValue);

        RuleFor(x => x.ResponseTimeCommitment)
            .MaximumLength(200).WithMessage("Response time commitment must not exceed 200 characters.")
            .When(x => x.ResponseTimeCommitment != null);
    }
}
