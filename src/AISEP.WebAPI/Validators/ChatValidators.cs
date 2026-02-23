using AISEP.Application.DTOs.Chat;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateConversationRequestValidator : AbstractValidator<CreateConversationRequest>
{
    public CreateConversationRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.MentorshipId.HasValue ^ x.ConnectionId.HasValue)
            .WithMessage("Exactly one of mentorshipId or connectionId must be provided.")
            .WithName("mentorshipId/connectionId");

        RuleFor(x => x.MentorshipId)
            .GreaterThan(0).When(x => x.MentorshipId.HasValue)
            .WithMessage("MentorshipId must be a positive integer.");

        RuleFor(x => x.ConnectionId)
            .GreaterThan(0).When(x => x.ConnectionId.HasValue)
            .WithMessage("ConnectionId must be a positive integer.");
    }
}

public class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be a positive integer.");

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Content) || !string.IsNullOrWhiteSpace(x.AttachmentUrl))
            .WithMessage("Either content or attachmentUrl must be provided.")
            .WithName("content/attachmentUrl");

        RuleFor(x => x.Content)
            .MaximumLength(4000).When(x => x.Content != null)
            .WithMessage("Content must not exceed 4000 characters.");

        RuleFor(x => x.AttachmentUrl)
            .MaximumLength(2048).When(x => x.AttachmentUrl != null)
            .WithMessage("AttachmentUrl must not exceed 2048 characters.");
    }
}

public class MarkReadAllRequestValidator : AbstractValidator<MarkReadAllRequest>
{
    public MarkReadAllRequestValidator()
    {
        RuleFor(x => x.ConversationId)
            .GreaterThan(0).WithMessage("ConversationId must be a positive integer.");
    }
}
