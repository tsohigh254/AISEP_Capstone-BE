using AISEP.Application.DTOs.Auth;
using FluentValidation;

namespace AISEP.WebAPI.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ")
            .MaximumLength(255).WithMessage("Email không được vượt quá 255 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số")
            .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống")
            .Equal(x => x.Password).WithMessage("Mật khẩu xác nhận không khớp");

        RuleFor(x => x.UserType)
            .NotEmpty().WithMessage("Loại người dùng không được để trống")
            .Must(x => new[] { "Startup", "Investor", "Advisor" }.Contains(x))
            .WithMessage("Loại người dùng phải là Startup, Investor hoặc Advisor");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token không được để trống");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống")
            .EmailAddress().WithMessage("Định dạng email không hợp lệ");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số")
            .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu mới không được để trống")
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không khớp");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải chứa ít nhất một chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải chứa ít nhất một chữ số")
            .Matches("[^a-zA-Z0-9]").WithMessage("Mật khẩu phải chứa ít nhất một ký tự đặc biệt")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");

        RuleFor(x => x.ConfirmNewPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu mới không được để trống")
            .Equal(x => x.NewPassword).WithMessage("Mật khẩu xác nhận không khớp");
    }
}
