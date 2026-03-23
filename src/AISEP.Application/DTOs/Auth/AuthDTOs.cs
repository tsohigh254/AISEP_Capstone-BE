namespace AISEP.Application.DTOs.Auth;

public class RegisterRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string ConfirmPassword { get; set; } = null!;
    public string UserType { get; set; } = null!; // Startup, Investor, Advisor
}

public class LoginRequest
{
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = null!;
}

public class AuthResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
}

public class AuthData
{
    public UserProfileResponse Info { get; set; } = null!;
    public string AccessToken { get; set; } = null!;
    public DateTime AccessTokenExpires { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmNewPassword { get; set; } = null!;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = null!;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
    public string ConfirmNewPassword { get; set; } = null!;
}

public class AdminResetPasswordRequest
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = null!;
}

public class EmailVerifyRequest
{
    public string Email { get; set; } = null!;
    public string Otp { get; set; } = null!;
}

public class ResendEmailRequest
{
    public string Email { get; set; } = null!;
}

