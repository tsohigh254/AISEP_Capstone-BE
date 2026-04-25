using AISEP.Application.DTOs.Investor;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class SubmitInvestorKYCRequestValidator : AbstractValidator<SubmitInvestorKYCRequest>
{
    private static readonly string[] AllowedCategories = { "INSTITUTIONAL", "INDIVIDUAL_ANGEL" };

    public SubmitInvestorKYCRequestValidator()
    {
        RuleFor(x => x.InvestorCategory)
            .NotEmpty().WithMessage("Loại nhà đầu tư không được để trống.")
            .Must(c => AllowedCategories.Contains(c))
            .WithMessage($"Loại nhà đầu tư phải là một trong: {string.Join(", ", AllowedCategories)}");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Email liên hệ không được để trống.")
            .EmailAddress().WithMessage("Email liên hệ phải có định dạng hợp lệ");
    }
}

public class SaveInvestorKYCDraftRequestValidator : AbstractValidator<SaveInvestorKYCDraftRequest>
{
    private static readonly string[] AllowedCategories = { "INSTITUTIONAL", "INDIVIDUAL_ANGEL" };

    public SaveInvestorKYCDraftRequestValidator()
    {
        RuleFor(x => x.InvestorCategory)
            .Must(c => c == null || AllowedCategories.Contains(c))
            .WithMessage($"Loại nhà đầu tư phải là một trong: {string.Join(", ", AllowedCategories)}");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("Email liên hệ phải có định dạng hợp lệ")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));
    }
}

public class CreateInvestorRequestValidator : AbstractValidator<CreateInvestorRequest>
{
    public CreateInvestorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.FirmName)
            .MaximumLength(200).WithMessage("Tên công ty/quỹ không được vượt quá 200 ký tự");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Chức danh không được vượt quá 200 ký tự");

        RuleFor(x => x.Bio)
            .MaximumLength(5000).WithMessage("Tiểu sử không được vượt quá 5000 ký tự");

        RuleFor(x => x.InvestmentThesis)
            .MaximumLength(5000).WithMessage("Luận điểm đầu tư không được vượt quá 5000 ký tự");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Địa điểm không được vượt quá 200 ký tự");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Quốc gia không được vượt quá 100 ký tự");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("Website không được vượt quá 500 ký tự");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("LinkedIn URL không được vượt quá 500 ký tự");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateInvestorRequestValidator : AbstractValidator<UpdateInvestorRequest>
{
    public UpdateInvestorRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.FirmName)
            .MaximumLength(200).WithMessage("Tên công ty/quỹ không được vượt quá 200 ký tự");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Chức danh không được vượt quá 200 ký tự");

        RuleFor(x => x.Bio)
            .MaximumLength(5000).WithMessage("Tiểu sử không được vượt quá 5000 ký tự");

        RuleFor(x => x.InvestmentThesis)
            .MaximumLength(5000).WithMessage("Investment thesis must not exceed 5000 characters");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Location must not exceed 200 characters");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website must be a valid URL")
            .MaximumLength(500).WithMessage("Website must not exceed 500 characters");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL must be a valid URL")
            .MaximumLength(500).WithMessage("LinkedIn URL must not exceed 500 characters");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{


    public UpdatePreferencesRequestValidator()
    {
        RuleFor(x => x.TicketMin)
            .GreaterThanOrEqualTo(0).When(x => x.TicketMin.HasValue)
            .WithMessage("Mức đầu tư tối thiểu phải >= 0");

        RuleFor(x => x.TicketMax)
            .GreaterThanOrEqualTo(0).When(x => x.TicketMax.HasValue)
            .WithMessage("Mức đầu tư tối đa phải >= 0");

        RuleFor(x => x)
            .Must(x => !x.TicketMin.HasValue || !x.TicketMax.HasValue || x.TicketMin <= x.TicketMax)
            .WithMessage("Mức đầu tư tối thiểu phải nhỏ hơn hoặc bằng tối đa")
            .WithName("TicketRange");


        RuleFor(x => x.PreferredGeographies)
            .MaximumLength(1000).WithMessage("Địa lý ưu tiên không được vượt quá 1000 ký tự");

        RuleFor(x => x.MinPotentialScore)
            .InclusiveBetween(0f, 100f).When(x => x.MinPotentialScore.HasValue)
            .WithMessage("Điểm tiềm năng tối thiểu phải từ 0 đến 100");

        RuleFor(x => x.PreferredStageIDs)
            .Must(ids => ids == null || ids.All(id => id > 0))
            .WithMessage("Mỗi StageID phải là số nguyên dương");

        RuleFor(x => x.PreferredIndustryIDs)
            .Must(ids => ids == null || ids.All(id => id > 0))
            .WithMessage("Mỗi IndustryID phải là số nguyên dương");
    }
}

public class WatchlistAddRequestValidator : AbstractValidator<WatchlistAddRequest>
{
    private static readonly string[] AllowedPriorities = { "Low", "Medium", "High" };

    public WatchlistAddRequestValidator()
    {
        RuleFor(x => x.StartupId)
            .GreaterThan(0).WithMessage("startupId phải lớn hơn 0");

        RuleFor(x => x.WatchReason)
            .MaximumLength(1000).WithMessage("Lý do theo dõi không được vượt quá 1000 ký tự");

        RuleFor(x => x.Priority)
            .Must(p => string.IsNullOrEmpty(p) || AllowedPriorities.Contains(p))
            .WithMessage($"Độ ưu tiên phải là một trong: {string.Join(", ", AllowedPriorities)}");
    }
}
