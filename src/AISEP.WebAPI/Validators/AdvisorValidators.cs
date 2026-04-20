using AISEP.Application.DTOs.Advisor;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateAdvisorRequestValidator : AbstractValidator<CreateAdvisorRequest>
{
    public CreateAdvisorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(120).WithMessage("Họ tên không được vượt quá 120 ký tự.");

        RuleFor(x => x.Title)
            .MaximumLength(120).WithMessage("Chức danh không được vượt quá 120 ký tự.")
            .When(x => x.Title != null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Tiểu sử không được vượt quá 2000 ký tự.")
            .When(x => x.Bio != null);


        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL phải là URL hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInURL));

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Triết lý cố vấn không được vượt quá 2000 ký tự.")
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
            .MaximumLength(120).WithMessage("Họ tên không được vượt quá 120 ký tự.")
            .When(x => x.FullName != null);

        RuleFor(x => x.Title)
            .MaximumLength(120).WithMessage("Chức danh không được vượt quá 120 ký tự.")
            .When(x => x.Title != null);

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Tiểu sử không được vượt quá 2000 ký tự.")
            .When(x => x.Bio != null);

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL phải là URL hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.LinkedInURL));

        RuleFor(x => x.MentorshipPhilosophy)
            .MaximumLength(2000).WithMessage("Triết lý cố vấn không được vượt quá 2000 ký tự.")
            .When(x => x.MentorshipPhilosophy != null);
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}


public class UpdateAvailabilityRequestValidator : AbstractValidator<UpdateAvailabilityRequest>
{
    public UpdateAvailabilityRequestValidator()
    {
        RuleFor(x => x.SessionFormats)
            .MaximumLength(500).WithMessage("Định dạng buổi gặp không được vượt quá 500 ký tự.")
            .When(x => x.SessionFormats != null);

        RuleFor(x => x.TypicalSessionDuration)
            .InclusiveBetween(15, 480).WithMessage("Thời lượng buổi gặp phải từ 15 đến 480 phút.")
            .When(x => x.TypicalSessionDuration.HasValue);

        RuleFor(x => x.WeeklyAvailableHours)
            .InclusiveBetween(1, 168).WithMessage("Số giờ sẵn sàng mỗi tuần phải từ 1 đến 168.")
            .When(x => x.WeeklyAvailableHours.HasValue);

        RuleFor(x => x.MaxConcurrentMentees)
            .InclusiveBetween(1, 100).WithMessage("Số mentee cùng lúc tối đa phải từ 1 đến 100.")
            .When(x => x.MaxConcurrentMentees.HasValue);

        RuleFor(x => x.ResponseTimeCommitment)
            .MaximumLength(200).WithMessage("Cam kết thời gian phản hồi không được vượt quá 200 ký tự.")
            .When(x => x.ResponseTimeCommitment != null);
    }
}
