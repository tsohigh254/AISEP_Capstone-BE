using AISEP.Application.DTOs.Document;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class DocumentCreateRequestValidator : AbstractValidator<DocumentCreateRequest>
{
    private static readonly string[] AllowedDocumentTypes =
        { "PitchDeck", "BusinessPlan", "Financials", "Legal", "Other" };

    public DocumentCreateRequestValidator()
    {
        RuleFor(x => x.DocumentType)
            .NotEmpty().WithMessage("DocumentType is required.")
            .Must(t => AllowedDocumentTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"DocumentType must be one of: {string.Join(", ", AllowedDocumentTypes)}.");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Version)
            .MaximumLength(20).WithMessage("Version must not exceed 20 characters.")
            .When(x => x.Version != null);
    }
}

public class DocumentUpdateMetadataRequestValidator : AbstractValidator<DocumentUpdateMetadataRequest>
{
    private static readonly string[] AllowedDocumentTypes =
        { "PitchDeck", "BusinessPlan", "Financials", "Legal", "Other" };

    public DocumentUpdateMetadataRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.DocumentType)
            .Must(t => AllowedDocumentTypes.Contains(t!, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"DocumentType must be one of: {string.Join(", ", AllowedDocumentTypes)}.")
            .When(x => x.DocumentType != null);
    }
}
