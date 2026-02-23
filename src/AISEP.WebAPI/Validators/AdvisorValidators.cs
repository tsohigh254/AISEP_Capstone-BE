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
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInURL));

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be >= 0.")
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Mentorship philosophy must not exceed 2000 characters.")
            .When(x => x.MentorshipPhilosophy != null);
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
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
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInURL));

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).WithMessage("Years of experience must be >= 0.")
            .When(x => x.YearsOfExperience.HasValue);

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Mentorship philosophy must not exceed 2000 characters.")
            .When(x => x.MentorshipPhilosophy != null);
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
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

            item.RuleFor(e => e.ProficiencyLevel)
                .MaximumLength(50).WithMessage("Proficiency level must not exceed 50 characters.")
                .When(e => e.ProficiencyLevel != null);
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
