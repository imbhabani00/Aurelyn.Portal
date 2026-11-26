using Aurelyn.Portal.Extension;
using Aurelyn.Portal.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Ecommerce Web...");

    var builder = WebApplication.CreateBuilder(args);

    #region Serilog

    builder.Host.UseSerilog((context, services, config) =>
    {
        config.ReadFrom.Configuration(context.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .WriteTo.Console();
    });

    #endregion

    #region MVC

    builder.Services.AddControllersWithViews();

    #endregion

    #region Distributed Cache

    builder.Services.AddDistributedMemoryCache();

    #endregion

    #region Session

    builder.Services.AddSessionConfig(builder.Configuration);

    #endregion

    #region HttpContextAccessor

    builder.Services.AddHttpContextAccessor();

    #endregion

    #region SignalR

    builder.Services.AddSignalR();

    #endregion

    #region Application Services

    builder.Services.AddApplicationServices(builder.Configuration);

    #endregion

    #region Routing

    builder.Services.AddRouting(options =>
    {
        options.LowercaseUrls = true;
        options.LowercaseQueryStrings = true;
    });

    #endregion

    #region Antiforgery

    builder.Services.AddAntiforgery(options =>
    {
        options.HeaderName = "RequestVerificationToken";
    });

    #endregion

    #region Authentication

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultScheme =
                CookieAuthenticationDefaults.AuthenticationScheme;

            options.DefaultAuthenticateScheme =
                CookieAuthenticationDefaults.AuthenticationScheme;

            options.DefaultChallengeScheme =
                CookieAuthenticationDefaults.AuthenticationScheme;

            options.DefaultSignInScheme =
                CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";

            options.ExpireTimeSpan =
                TimeSpan.FromMinutes(10);

            options.SlidingExpiration = true;

            options.Cookie.HttpOnly = true;

            options.Cookie.SecurePolicy =
                CookieSecurePolicy.Always;

            options.Cookie.SameSite =
                SameSiteMode.Strict;
        });

    #endregion

    var app = builder.Build();

    #region Exception Handling

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    #endregion

    #region Middleware Pipeline

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseStatusCodePagesWithReExecute(
        "/Error/Index",
        "?code={0}");

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseRouting();

    // Session must be available before custom middleware.
    app.UseSession();

    // Authenticate the current Web application user.
    app.UseAuthentication();

    // Apply authorization policies.
    app.UseAuthorization();

    // Custom middleware can now access authenticated user information.
    app.UseMiddleware<AuthMiddleware>();

    app.UseMiddleware<ProfileGateMiddleware>();

    #endregion

    #region Endpoints

    app.MapControllerRoute(
        name: "tenant",
        pattern: "t/{tenant}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Account}/{action=Login}/{id?}");

    #endregion

    Log.Information("Ecommerce Web started successfully.");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Web app failed to start");
}
finally
{
    Log.CloseAndFlush();
}
