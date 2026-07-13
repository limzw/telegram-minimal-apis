namespace TelegramMinimalAPIs.Common.Services.RuntimeUser
{
    public enum InitialisationStatus
    {
        PendingPhoneNumber,
        PendingAuthorisationCode,
        Pending2FAVerificationCode,
        Ready,
    }
}
