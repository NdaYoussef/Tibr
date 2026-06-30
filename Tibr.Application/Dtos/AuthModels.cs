

namespace Tibr.Application.Dtos
{
    public record AuthResponse(bool IsSuccess, string MessageAR, string MessageEN, string? Token = null, DateTime? Expiration = null, string? userId = null);

    public record RegisterRequestData(string FirstName, string LastName, string Email, string Phone, string Password, string ConfirmPassword,string? lang);

    public record LoginRequestData(string Email, string Password, bool RememberMe);

    public record VerifyOtpRequest(string Email, string Otp);

    public record ForgotPasswordRequestData(string Email);

    public record ResetPasswordRequestData(string Email, string Otp, string NewPassword, string ConfirmPassword);

    public class UserProfileDto
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string KycStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
