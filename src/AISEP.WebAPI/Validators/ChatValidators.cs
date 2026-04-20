using AISEP.Application.DTOs.Chat;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.MentorshipId.HasValue ^ x.ConnectionId.HasValue)
            .WithMessage("Phải cung cấp đúng một trong hai: mentorshipId hoặc connectionId.")
            .WithName("mentorshipId/connectionId");

        RuleFor(x => x.MentorshipId)
            .GreaterThan(0).When(x => x.MentorshipId.HasValue)
            .WithMessage("MentorshipId phải là số nguyên dương.");

        RuleFor(x => x.ConnectionId)
            .GreaterThan(0).When(x => x.ConnectionId.HasValue)
            .WithMessage("ConnectionId phải là số nguyên dương.");
    }
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId phải là số nguyên dương.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.AttachmentUrl))
            .WithMessage("Phải cung cấp ít nhất một trong hai: nội dung hoặc tệp đính kèm.")
            .WithName("content/attachmentUrl");

        RuleFor(x => x.Content)
            .MaximumLength(4000).When(x => x.Content != null)
            .WithMessage("Nội dung không được vượt quá 4000 ký tự.");

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(2048).When(x => x.AttachmentUrl != null)
            .WithMessage("AttachmentUrl không được vượt quá 2048 ký tự.");
    }
}

public class MarkReadAllRequestValidator : AbstractValidator<MarkReadAllRequest>
{
    public MarkReadAllRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId phải là số nguyên dương.");
    }
}
