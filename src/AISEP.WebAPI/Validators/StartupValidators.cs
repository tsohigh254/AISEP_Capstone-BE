using AISEP.Application.DTOs.Startup;
using AISEP.Domain.Enums;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateStartupRequestValidator : AbstractValidator<CreateStartupRequest>
{


    public CreateStartupRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Tên công ty không được để trống")
            .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự");

        RuleFor(x => x.IndustryID)
            .GreaterThan(0).When(x => x.IndustryID.HasValue)
            .WithMessage("IndustryID phải là số nguyên dương");

        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Mô tả không được vượt quá 5000 ký tự");

        RuleFor(x => x.SubIndustryID)
            .GreaterThan(0).When(x => x.SubIndustryID.HasValue)
            .WithMessage("SubIndustryID phải là số nguyên dương");

        RuleFor(x => x.StageID)
            .GreaterThan(0).When(x => x.StageID.HasValue)
            .WithMessage("StageID phải là số nguyên dương");

        RuleFor(x => x.ProductStatus)
            .MaximumLength(100).WithMessage("Trạng thái sản phẩm không được vượt quá 100 ký tự");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Địa điểm không được vượt quá 200 ký tự");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Quốc gia không được vượt quá 100 ký tự");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("Website không được vượt quá 500 ký tự");

        RuleFor(x => x.PitchDeckUrl)
            .Must(BeAValidUrlOrNull).WithMessage("PitchDeckUrl phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("PitchDeckUrl không được vượt quá 500 ký tự");

        RuleFor(x => x.MetricSummary)
            .MaximumLength(1000).WithMessage("Tóm tắt số liệu không được vượt quá 1000 ký tự");

        RuleFor(x => x.TeamSize)
            .MaximumLength(50).WithMessage("Quy mô nhóm không được vượt quá 50 ký tự");

        RuleFor(x => x.FundingAmountSought)
            .GreaterThanOrEqualTo(0).When(x => x.FundingAmountSought.HasValue)
            .WithMessage("Số tiền kêu gọi đầu tư phải >= 0");

        RuleFor(x => x.CurrentFundingRaised)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentFundingRaised.HasValue)
            .WithMessage("Số tiền đã huy động phải >= 0");

        RuleFor(x => x.Valuation)
            .GreaterThanOrEqualTo(0).When(x => x.Valuation.HasValue)
            .WithMessage("Định giá phải >= 0");

    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateStartupRequestValidator : AbstractValidator<UpdateStartupRequest>
{


    public UpdateStartupRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự")
            .NotEmpty().When(x => x.CompanyName != null).WithMessage("Tên công ty không được để trống");

        RuleFor(x => x.IndustryID)
            .GreaterThan(0).When(x => x.IndustryID.HasValue)
            .WithMessage("IndustryID phải là số nguyên dương");


        RuleFor(x => x.Description)
            .MaximumLength(5000).WithMessage("Mô tả không được vượt quá 5000 ký tự");

        RuleFor(x => x.SubIndustryID)
            .GreaterThan(0).When(x => x.SubIndustryID.HasValue)
            .WithMessage("SubIndustryID phải là số nguyên dương");

        RuleFor(x => x.StageID)
            .GreaterThan(0).When(x => x.StageID.HasValue)
            .WithMessage("StageID phải là số nguyên dương");

        RuleFor(x => x.ProductStatus)
            .MaximumLength(100).WithMessage("Trạng thái sản phẩm không được vượt quá 100 ký tự");

        RuleFor(x => x.Location)
            .MaximumLength(200).WithMessage("Địa điểm không được vượt quá 200 ký tự");

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Quốc gia không được vượt quá 100 ký tự");

        RuleFor(x => x.Website)
            .Must(BeAValidUrlOrNull).WithMessage("Website phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("Website không được vượt quá 500 ký tự");

        RuleFor(x => x.PitchDeckUrl)
            .Must(BeAValidUrlOrNull).WithMessage("PitchDeckUrl phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("PitchDeckUrl không được vượt quá 500 ký tự");

        RuleFor(x => x.MetricSummary)
            .MaximumLength(1000).WithMessage("Tóm tắt số liệu không được vượt quá 1000 ký tự");

        RuleFor(x => x.TeamSize)
            .MaximumLength(50).WithMessage("Quy mô nhóm không được vượt quá 50 ký tự");


        RuleFor(x => x.FundingAmountSought)
            .GreaterThanOrEqualTo(0).When(x => x.FundingAmountSought.HasValue)
            .WithMessage("Số tiền kêu gọi đầu tư phải >= 0");

        RuleFor(x => x.CurrentFundingRaised)
            .GreaterThanOrEqualTo(0).When(x => x.CurrentFundingRaised.HasValue)
            .WithMessage("Số tiền đã huy động phải >= 0");

        RuleFor(x => x.Valuation)
            .GreaterThanOrEqualTo(0).When(x => x.Valuation.HasValue)
            .WithMessage("Định giá phải >= 0");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class CreateTeamMemberRequestValidator : AbstractValidator<CreateTeamMemberRequest>
{
    public CreateTeamMemberRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống")
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Vai trò không được để trống")
            .MaximumLength(100).WithMessage("Vai trò không được vượt quá 100 ký tự");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Chức danh không được vượt quá 200 ký tự");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("LinkedIn URL không được vượt quá 500 ký tự");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Tiểu sử không được vượt quá 2000 ký tự");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue)
            .WithMessage("Số năm kinh nghiệm phải >= 0");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateTeamMemberRequestValidator : AbstractValidator<UpdateTeamMemberRequest>
{
    public UpdateTeamMemberRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(200).WithMessage("Họ tên không được vượt quá 200 ký tự")
            .NotEmpty().When(x => x.FullName != null).WithMessage("Họ tên không được để trống");

        RuleFor(x => x.Role)
            .MaximumLength(100).WithMessage("Vai trò không được vượt quá 100 ký tự");

        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Chức danh không được vượt quá 200 ký tự");

        RuleFor(x => x.LinkedInURL)
            .Must(BeAValidUrlOrNull).WithMessage("LinkedIn URL phải là URL hợp lệ")
            .MaximumLength(500).WithMessage("LinkedIn URL không được vượt quá 500 ký tự");

        RuleFor(x => x.Bio)
            .MaximumLength(2000).WithMessage("Tiểu sử không được vượt quá 2000 ký tự");

        RuleFor(x => x.YearsOfExperience)
            .GreaterThanOrEqualTo(0).When(x => x.YearsOfExperience.HasValue)
            .WithMessage("Số năm kinh nghiệm phải >= 0");
    }

    private static bool BeAValidUrlOrNull(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
