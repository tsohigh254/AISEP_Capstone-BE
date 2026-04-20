using AISEP.Application.DTOs.Moderation;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateFlagRequestValidator : AbstractValidator<CreateFlagRequest>
{
    public CreateFlagRequestValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Loại đối tượng không được để trống.")
            .MaximumLength(50).WithMessage("Loại đối tượng không được vượt quá 50 ký tự.");

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage("EntityId phải lớn hơn 0.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do không được để trống.")
            .MaximumLength(200).WithMessage("Lý do không được vượt quá 200 ký tự.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .When(x => x.Description != null);
    }
}

public class AssignFlagRequestValidator : AbstractValidator<AssignFlagRequest>
{
    public AssignFlagRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => x.Note != null);
    }
}

public class ResolveFlagRequestValidator : AbstractValidator<ResolveFlagRequest>
{
    private static readonly string[] AllowedDecisions = { "MarkSafe", "RejectReport", "Resolved" };

    public ResolveFlagRequestValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty().WithMessage("Quyết định không được để trống.")
            .Must(d => AllowedDecisions.Contains(d))
            .WithMessage($"Quyết định phải là một trong: {string.Join(", ", AllowedDecisions)}.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
            .When(x => x.Note != null);
    }
}

public class CreateModerationActionRequestValidator : AbstractValidator<CreateModerationActionRequest>
{
    private static readonly string[] AllowedActionTypes =
        { "Warn", "Hide", "Remove", "LockUser", "UnlockUser", "BanUser", "MarkSafe", "RejectReport" };

    public CreateModerationActionRequestValidator()
    {
        RuleFor(x => x.ActionType)
            .NotEmpty().WithMessage("Loại hành động không được để trống.")
            .Must(t => AllowedActionTypes.Contains(t))
            .WithMessage($"Loại hành động phải là một trong: {string.Join(", ", AllowedActionTypes)}.");

        RuleFor(x => x.ActionNote)
            .MaximumLength(2000).WithMessage("Ghi chú hành động không được vượt quá 2000 ký tự.")
            .When(x => x.ActionNote != null);

        RuleFor(x => x.DurationDays)
            .GreaterThanOrEqualTo(1).WithMessage("Số ngày phải ít nhất là 1.")
            .When(x => x.DurationDays.HasValue);
    }
}
