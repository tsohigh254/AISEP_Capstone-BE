using AISEP.Application.DTOs.Connection;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class CreateConnectionRequestValidator : AbstractValidator<CreateConnectionRequest>
{
    public CreateConnectionRequestValidator()
    {
        RuleFor(x => x.StartupId)
            .GreaterThan(0).WithMessage("StartupId phải là số nguyên dương.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Lời nhắn không được để trống.")
            .MaximumLength(300).WithMessage("Lời nhắn tối đa 300 ký tự.");
    }
}

public class UpdateConnectionRequestValidator : AbstractValidator<UpdateConnectionRequest>
{
    public UpdateConnectionRequestValidator()
    {
        RuleFor(x => x.Message)
            .MaximumLength(2000).WithMessage("Lời nhắn không được vượt quá 2000 ký tự.")
            .When(x => x.Message != null);
    }
}

public class RejectConnectionRequestValidator : AbstractValidator<RejectConnectionRequest>
{
    public RejectConnectionRequestValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(1000).WithMessage("Lý do không được vượt quá 1000 ký tự.")
            .When(x => x.Reason != null);
    }
}

public class CreateInfoRequestValidator : AbstractValidator<CreateInfoRequest>
{
    public CreateInfoRequestValidator()
    {
        RuleFor(x => x.RequestType)
            .NotEmpty().WithMessage("Loại yêu cầu không được để trống.")
            .MaximumLength(200).WithMessage("Loại yêu cầu không được vượt quá 200 ký tự.");

        RuleFor(x => x.RequestMessage)
            .MaximumLength(2000).WithMessage("Nội dung yêu cầu không được vượt quá 2000 ký tự.")
            .When(x => x.RequestMessage != null);
    }
}

public class FulfillInfoRequestValidator : AbstractValidator<FulfillInfoRequest>
{
    public FulfillInfoRequestValidator()
    {
        RuleFor(x => x.ResponseMessage)
            .MaximumLength(4000).WithMessage("Nội dung phản hồi không được vượt quá 4000 ký tự.")
            .When(x => x.ResponseMessage != null);
    }
}

public class CreatePortfolioCompanyRequestValidator : AbstractValidator<CreatePortfolioCompanyRequest>
{
    public CreatePortfolioCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Tên công ty không được để trống.")
            .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự.");

        RuleFor(x => x.Industry)
            .MaximumLength(200).WithMessage("Ngành không được vượt quá 200 ký tự.")
            .When(x => x.Industry != null);

        RuleFor(x => x.InvestmentStage)
            .MaximumLength(100).WithMessage("Giai đoạn đầu tư không được vượt quá 100 ký tự.")
            .When(x => x.InvestmentStage != null);

        RuleFor(x => x.InvestmentAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền đầu tư phải >= 0.")
            .When(x => x.InvestmentAmount.HasValue);

        RuleFor(x => x.CurrentStatus)
            .MaximumLength(100).WithMessage("Trạng thái không được vượt quá 100 ký tự.")
            .When(x => x.CurrentStatus != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .When(x => x.Description != null);
    }
}

public class UpdatePortfolioCompanyRequestValidator : AbstractValidator<UpdatePortfolioCompanyRequest>
{
    public UpdatePortfolioCompanyRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .MaximumLength(200).WithMessage("Tên công ty không được vượt quá 200 ký tự.")
            .When(x => x.CompanyName != null);

        RuleFor(x => x.Industry)
            .MaximumLength(200).WithMessage("Ngành không được vượt quá 200 ký tự.")
            .When(x => x.Industry != null);

        RuleFor(x => x.InvestmentStage)
            .MaximumLength(100).WithMessage("Giai đoạn đầu tư không được vượt quá 100 ký tự.")
            .When(x => x.InvestmentStage != null);

        RuleFor(x => x.InvestmentAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Số tiền đầu tư phải >= 0.")
            .When(x => x.InvestmentAmount.HasValue);

        RuleFor(x => x.CurrentStatus)
            .MaximumLength(100).WithMessage("Trạng thái không được vượt quá 100 ký tự.")
            .When(x => x.CurrentStatus != null);

        RuleFor(x => x.ExitType)
            .MaximumLength(100).WithMessage("Loại thoái vốn không được vượt quá 100 ký tự.")
            .When(x => x.ExitType != null);

        RuleFor(x => x.ExitValue)
            .GreaterThanOrEqualTo(0).WithMessage("Giá trị thoái vốn phải >= 0.")
            .When(x => x.ExitValue.HasValue);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được vượt quá 2000 ký tự.")
            .When(x => x.Description != null);
    }
}
