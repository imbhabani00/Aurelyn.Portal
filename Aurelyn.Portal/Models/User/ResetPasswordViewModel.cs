namespace Aurelyn.Portal.Models.User
{
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }
        public string? TenantCode { get; set; }
        public string NewPassword { get; set; }
        public string Code { get; set; }
        public string ConfirmPassword { get; set; }
    }
}