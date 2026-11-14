namespace Aurelyn.Portal.Models.User
{
    public class UserDetails
    {
        public int TenantId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Initials { get; set; }
    }
}