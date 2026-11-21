using Aurelyn.Portal.Models.User;

namespace Aurelyn.Portal.Helper
{
    public static class ProfileHelper
    {
        public static bool IsComplete(UserDetails p) =>
            p != null &&
            !string.IsNullOrWhiteSpace(p.FirstName) && !string.IsNullOrWhiteSpace(p.LastName) &&
            !string.IsNullOrWhiteSpace(p.Email) && !string.IsNullOrWhiteSpace(p.PhoneNumber) &&
            !string.IsNullOrWhiteSpace(p.AddressLine1) && !string.IsNullOrWhiteSpace(p.City) &&
            !string.IsNullOrWhiteSpace(p.State) && !string.IsNullOrWhiteSpace(p.ZipCode) &&
            !string.IsNullOrWhiteSpace(p.Country);
    }
}
