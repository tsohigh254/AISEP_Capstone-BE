using AISEP.Application.DTOs.Notification;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class MarkReadRequestValidator : AbstractValidator<MarkReadRequest>
{
    public MarkReadRequestValidator()
    {
        // IsRead is optional (defaults to true when null) — no rules needed.
        // Validator registered so FluentValidation auto-validation detects the type.
    }
}
