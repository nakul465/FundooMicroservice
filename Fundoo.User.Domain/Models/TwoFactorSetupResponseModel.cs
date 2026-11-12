namespace Fundoo.User.Domain.Models
{
    public class TwoFactorSetupResponseModel
    {
        public string SecretKey { get; set; } = string.Empty;
        public string QrCodeUri { get; set; } = string.Empty;
        public string ManualEntryKey { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
