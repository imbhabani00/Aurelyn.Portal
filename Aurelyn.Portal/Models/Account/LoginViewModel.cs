namespace Aurelyn.Portal.Models.Account
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public string TenantCode { get; set; }
        public string ReturnUrl { get; set; }
    }
}