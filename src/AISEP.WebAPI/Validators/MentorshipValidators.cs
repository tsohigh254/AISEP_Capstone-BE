using AISEP.Application.DTOs.Mentorship;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateMentorshipRequestValidator : AbstractValidator<CreateMentorshipRequest>
{
    public CreateMentorshipRequestValidator()
    {
        RuleFor(x => x.AdvisorId)
            .GreaterThan(0).WithMessage("AdvisorId phải là số nguyên dương.");

        RuleFor(x => x.ChallengeDescription)
            .NotEmpty().WithMessage("Mô tả thách thức không được để trống.")
            .MaximumLength(2000).WithMessage("Mô tả thách thức không được vượt quá 2000 ký tự.");

        RuleFor(x => x.SpecificQuestions)
            .MaximumLength(2000).WithMessage("Câu hỏi cụ thể không được vượt quá 2000 ký tự.")
            .When(x => x.SpecificQuestions != null);

        RuleFor(x => x.PreferredFormat)
            .MaximumLength(100).WithMessage("Định dạng ưa thích không được vượt quá 100 ký tự.")
            .When(x => x.PreferredFormat != null);

        RuleFor(x => x.ExpectedDuration)
            .MaximumLength(200).WithMessage("Thời lượng dự kiến không được vượt quá 200 ký tự.")
            .When(x => x.ExpectedDuration != null);

        RuleFor(x => x.ExpectedScope)
            .MaximumLength(500).WithMessage("Phạm vi dự kiến không được vượt quá 500 ký tự.")
            .When(x => x.ExpectedScope != null);
    }
}

public class RejectMentorshipRequestValidator : AbstractValidator<RejectMentorshipRequest>
{
    public RejectMentorshipRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Lý do không được vượt quá 1000 ký tự.")
            .When(x => x.Reason != null);
    }
}

public class CreateSessionRequestValidator : AbstractValidator<CreateSessionRequest>
{
    public CreateSessionRequestValidator()
    {
        RuleFor(x => x.ScheduledStartAt)
            .GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("Thời gian bắt đầu phải ở trong tương lai.");

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(15, 480)
            .WithMessage("Thời lượng phải từ 15 đến 480 phút.");

        RuleFor(x => x.SessionFormat)
            .MaximumLength(100).WithMessage("Định dạng buổi gặp không được vượt quá 100 ký tự.")
            .When(x => x.SessionFormat != null);

        RuleFor(x => x.MeetingUrl)
            .Must(BeAValidUrlOrNull).WithMessage("URL cuộc họ p phải là URL hợp lệ.")
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
            .WithMessage("Thời lượng phải từ 15 đến 480 phút.")
            .When(x => x.DurationMinutes.HasValue);

        RuleFor(x => x.SessionFormat)
            .MaximumLength(100).WithMessage("Định dạng buổi gặp không được vượt quá 100 ký tự.")
            .When(x => x.SessionFormat != null);

        RuleFor(x => x.MeetingUrl)
            .Must(BeAValidUrlOrNull).WithMessage("URL cuộc họ p phải là URL hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.MeetingUrl));

        RuleFor(x => x.SessionStatus)
            .Must(s => s == null || new[] { "Scheduled", "InProgress", "Completed", "Cancelled" }.Contains(s))
            .WithMessage("Trạng thái buổi gặp phải là một trong: Scheduled, InProgress, Completed, Cancelled.");

        RuleFor(x => x.TopicsDiscussed)
            .MaximumLength(2000).WithMessage("Chủ đề thảo luận không được vượt quá 2000 ký tự.")
            .When(x => x.TopicsDiscussed != null);

        RuleFor(x => x.KeyInsights)
            .MaximumLength(2000).WithMessage("Nhận xét chính không được vượt quá 2000 ký tự.")
            .When(x => x.KeyInsights != null);

        RuleFor(x => x.ActionItems)
            .MaximumLength(2000).WithMessage("Hành động cần thực hiện không được vượt quá 2000 ký tự.")
            .When(x => x.ActionItems != null);

        RuleFor(x => x.NextSteps)
            .MaximumLength(2000).WithMessage("Các bước tiếp theo không được vượt quá 2000 ký tự.")
            .When(x => x.NextSteps != null);
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
        RuleFor(x => x.SessionId)
            .NotNull().WithMessage("SessionID không được để trống.")
            .GreaterThan(0).WithMessage("SessionID phải là số nguyên dương.")
            .When(x => x.SessionId.HasValue);

        RuleFor(x => x.ReportSummary)
            .NotEmpty().WithMessage("Tóm tắt báo cáo không được để trống.")
            .MaximumLength(2000).WithMessage("Tóm tắt báo cáo không được vượt quá 2000 ký tự.");

        RuleFor(x => x.DetailedFindings)
            .MaximumLength(5000).WithMessage("Phát hiện chi tiết không được vượt quá 5000 ký tự.")
            .When(x => x.DetailedFindings != null);

        RuleFor(x => x.Recommendations)
            .MaximumLength(2000).WithMessage("Khuyến nghị không được vượt quá 2000 ký tự.")
            .When(x => x.Recommendations != null);
    }
}

public class CreateFeedbackRequestValidator : AbstractValidator<CreateFeedbackRequest>
{
    public CreateFeedbackRequestValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Đánh giá phải từ 1 đến 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000).WithMessage("Nhận xét không được vượt quá 2000 ký tự.")
            .When(x => x.Comment != null);
    }
}

public class ReviewReportRequestValidator : AbstractValidator<ReviewReportRequest>
{
    public ReviewReportRequestValidator()
    {
        RuleFor(x => x.ReviewStatus)
            .NotEmpty().WithMessage("Trạng thái đánh giá không được để trống.")
            .Must(s => s is "Passed" or "Failed" or "NeedsMoreInfo")
            .WithMessage("Trạng thái đánh giá phải là Passed, Failed hoặc NeedsMoreInfo.");

        RuleFor(x => x.Note)
            .MaximumLength(2000).WithMessage("Ghi chú không được vượt quá 2000 ký tự.")
            .When(x => x.Note != null);
    }
}

public class StaffMarkDisputeRequestValidator : AbstractValidator<StaffMarkDisputeRequest>
{
    public StaffMarkDisputeRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do tranh chấp không được để trống.")
            .MaximumLength(2000).WithMessage("Lý do không được vượt quá 2000 ký tự.");
    }
}

public class ResolveDisputeRequestValidator : AbstractValidator<ResolveDisputeRequest>
{
    public ResolveDisputeRequestValidator()
    {
        RuleFor(x => x.Resolution)
            .NotEmpty().WithMessage("Giải pháp không được để trống.")
            .MaximumLength(2000).WithMessage("Giải pháp không được vượt quá 2000 ký tự.");
    }
}
