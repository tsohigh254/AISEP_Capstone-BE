using AISEP.Application.DTOs.Moderation;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateFlagRequestValidator : AbstractValidator<CreateFlagRequest>
{
    public CreateFlagRequestValidator()
    {
        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("EntityType is required.")
            .MaximumLength(50).WithMessage("EntityType must be at most 50 characters.");

        RuleFor(x => x.EntityId)
            .GreaterThan(0).WithMessage("EntityId must be greater than 0.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(200).WithMessage("Reason must be at most 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be at most 2000 characters.")
            .When(x => x.Description != null);
    }
}

public class AssignFlagRequestValidator : AbstractValidator<AssignFlagRequest>
{
    public AssignFlagRequestValidator()
    {
        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note must be at most 1000 characters.")
            .When(x => x.Note != null);
    }
}

public class ResolveFlagRequestValidator : AbstractValidator<ResolveFlagRequest>
{
    private static readonly string[] AllowedDecisions = { "MarkSafe", "RejectReport", "Resolved" };

    public ResolveFlagRequestValidator()
    {
        RuleFor(x => x.Decision)
            .NotEmpty().WithMessage("Decision is required.")
            .Must(d => AllowedDecisions.Contains(d))
            .WithMessage($"Decision must be one of: {string.Join(", ", AllowedDecisions)}.");

        RuleFor(x => x.Note)
            .MaximumLength(1000).WithMessage("Note must be at most 1000 characters.")
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
            .NotEmpty().WithMessage("ActionType is required.")
            .Must(t => AllowedActionTypes.Contains(t))
            .WithMessage($"ActionType must be one of: {string.Join(", ", AllowedActionTypes)}.");

        RuleFor(x => x.ActionNote)
            .MaximumLength(2000).WithMessage("ActionNote must be at most 2000 characters.")
            .When(x => x.ActionNote != null);

        RuleFor(x => x.DurationDays)
            .GreaterThanOrEqualTo(1).WithMessage("DurationDays must be at least 1.")
            .When(x => x.DurationDays.HasValue);
    }
}
