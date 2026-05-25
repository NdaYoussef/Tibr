

namespace Tibr.Application.Dtos
{
    public record AuthResponse(bool IsSuccess, string MessageAR, string MessageEN, string? Token = null, DateTime? Expiration = null, string? userId = null);

    public record RegisterRequestData(string FirstName, string LastName, string Email, string Phone, string Password, string ConfirmPassword);

    public record LoginRequestData(string Email, string Password, bool RememberMe);

    public record VerifyOtpRequest(string Email, string Otp);

    public record ForgotPasswordRequestData(string Email);

    public record ResetPasswordRequestData(string Email, string Otp, string NewPassword, string ConfirmPassword);
}
