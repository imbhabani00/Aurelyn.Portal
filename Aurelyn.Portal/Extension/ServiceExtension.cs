using Aurelyn.Portal.Configurations;
using Microsoft.Extensions.Options;

namespace Aurelyn.Portal.Extension
{
    public static class ServiceExtension
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<AppSettings>(configuration);
            services.AddSingleton(sp =>
                sp.GetRequiredService<IOptions<AppSettings>>().Value);
            services.AddHttpClient("HealthCareApi", client =>
            {
                client.BaseAddress = new Uri(
                    configuration["HealthCareApi:BaseUrl"]!);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            var webAssembly = typeof(Program).Assembly;
            foreach (var type in webAssembly.GetTypes()
                .Where(x => x.IsClass
                         && !x.IsAbstract
                         && x.Name.EndsWith("Service")
                         && x.Name != "BaseService"))
            {
                var interfaceType = type.GetInterfaces()
                    .FirstOrDefault(i =>
                        i.Name != "IDisposable" &&
                        i.Name != "IBaseService");

                if (interfaceType != null)
                    services.AddScoped(interfaceType, type);
            }
            return services;
        }
    }
}