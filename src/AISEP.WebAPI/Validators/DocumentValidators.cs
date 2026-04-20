using AISEP.Application.DTOs.Document;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class DocumentCreateRequestValidator : AbstractValidator<DocumentCreateRequest>
{
    private static readonly string[] AllowedDocumentTypes =
        { "PitchDeck", "BusinessPlan", "Financials", "Legal", "Other" };

    public DocumentCreateRequestValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.")
            .When(x => x.Title != null);

        RuleFor(x => x.Version)
            .MaximumLength(20).WithMessage("Phiên bản không được vượt quá 20 ký tự.")
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
            .MaximumLength(200).WithMessage("Tiêu đề không được vượt quá 200 ký tự.")
            .When(x => x.Title != null);
    }
}
