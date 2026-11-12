namespace Fundoo.User.Application.Services
{
    public interface ITwoFactorService
    {
        string GenerateSecretKey();
        string GenerateQrCodeUri(string email, string secret);
        bool ValidateTotpCode(string secret, string code);
        string GenerateEmailOtp(string email);
        bool ValidateEmailOtp(string email, string code);
    }
}
