using AISEP.Application.DTOs.Mentorship;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateMentorshipRequestValidator : AbstractValidator<CreateMentorshipRequest>
{
    public CreateMentorshipRequestValidator()
    {
        RuleFor(x => x.AdvisorId)
            .GreaterThan(0).WithMessage("AdvisorId must be a positive integer.");

        RuleFor(x => x.ChallengeDescription)
            .NotEmpty().WithMessage("Challenge description is required.")
            .MaximumLength(2000).WithMessage("Challenge description must not exceed 2000 characters.");

        RuleFor(x => x.SpecificQuestions)
            .MaximumLength(2000).WithMessage("Specific questions must not exceed 2000 characters.")
            .When(x => x.SpecificQuestions != null);

        RuleFor(x => x.ExpectedDuration)
            .MaximumLength(200).WithMessage("Expected duration must not exceed 200 characters.")
            .When(x => x.ExpectedDuration != null);

        RuleFor(x => x.ExpectedScope)
            .MaximumLength(500).WithMessage("Expected scope must not exceed 500 characters.")
            .When(x => x.ExpectedScope != null);
    }
}

public class RejectMentorshipRequestValidator : AbstractValidator<RejectMentorshipRequest>
{
    public RejectMentorshipRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.")
            .When(x => x.Reason != null);
    }
}

public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.ScheduledStartAt)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Scheduled start time must be in the future.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480)
            .WithMessage("Duration must be between 15 and 480 minutes.");

        RuleFor(x => x.MeetingUrl)
            .Must(BeAValidUrlOrNull).WithMessage("Meeting URL must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateSessionRequestValidator : AbstractValidator<UpdateSessionRequest>
{
    public UpdateSessionRequestValidator()
    {
        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480)
            .WithMessage("Duration must be between 15 and 480 minutes.")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.MeetingUrl)
            .Must(BeAValidUrlOrNull).WithMessage("Meeting URL must be a valid URL.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));

        RuleFor(x => x.TopicsDiscussed)
            .MaximumLength(2000).WithMessage("Topics discussed must not exceed 2000 characters.")
            .When(x => x.TopicsDiscussed != null);
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportRequestValidator()
    {
        RuleFor(x => x.ReportSummary)
            .NotEmpty().WithMessage("Report summary is required.")
            .MaximumLength(2000).WithMessage("Report summary must not exceed 2000 characters.");

        RuleFor(x => x.DetailedFindings)
            .MaximumLength(5000).WithMessage("Detailed findings must not exceed 5000 characters.")
            .When(x => x.DetailedFindings != null);

        RuleFor(x => x.Recommendations)
            .MaximumLength(2000).WithMessage("Recommendations must not exceed 2000 characters.")
            .When(x => x.Recommendations != null);
    }
}

public class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Comment must not exceed 2000 characters.")
            .When(x => x.Comment != null);
    }
}
