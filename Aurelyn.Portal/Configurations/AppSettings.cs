namespace Aurelyn.Portal.Configurations
{
    public class AppSettings
    {
        public HealthCareApiSettings HealthCareApi { get; set; } = new();
        //public EmailSettings EmailSettings { get; set; } = new();
        public ConnectionStringsSettings ConnectionStrings { get; set; } = new();
        public SessionSettings Session { get; set; } = new();
    }

    public class HealthCareApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Version { get; set; } = "1";
    }

    public class ConnectionStringsSettings
    {
        public string ShopEaseConnection { get; set; } = string.Empty;
    }

    public class SessionSettings
    {
        public int TimeoutMinutes { get; set; } = 60;
        public string CookieName { get; set; } = "HealthCare.Web.Session";
    }
}
