using Aurelyn.Portal.Constants;
using Aurelyn.Portal.Helper;
using Aurelyn.Portal.Models.Account;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Models.Otp;
using Aurelyn.Portal.Models.User;
using Aurelyn.Portal.Service.Account;
using Aurelyn.Portal.Service.User;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShopEase.WebApp.Helpers;

namespace Aurelyn.App.Controllers
{
    public class AccountController : BaseController
    {
        #region Properties
        private readonly IAccountService _accountService;
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;
        private const string PendingOtpIdKey = "PendingOtpId";
        private const string PendingEmailKey = "PendingEmail";
        #endregion

        #region Constructor
        public AccountController(
            IAccountService accountService,
            IUserService userService,
            ILogger<AccountController> logger)
        {
            _accountService = accountService;
            _userService = userService;
            _logger = logger;
        }
        #endregion

        #region Login GET (Sign in + Sign up page)
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(AccessToken))
                return Redirect(RouteConstants.Dashboard);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }
        #endregion

        #region Login POST (Step 1: email + password -> OTP sent)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var apiResponse = new ApiResponse();

            try
            {
                var result = await _accountService.LoginAsync(model);

                if (!TryReadOtpResponse(result, out var otp, apiResponse))
                    return new ObjectResult(apiResponse);

                StorePendingOtp(otp!);

                _logger.LogInformation("Login: OTP sent");

                apiResponse.Status = true;
                apiResponse.Message = "A verification code was sent to your email.";
                apiResponse.ReturnUrl = RouteConstants.VerifyOtp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region Register POST (Sign up -> OTP sent)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            var apiResponse = new ApiResponse();
            try
            {
                var result = await _accountService.RegisterAsync(model);
                if (!TryReadOtpResponse(result, out var otp, apiResponse))
                    return new ObjectResult(apiResponse);
                StorePendingOtp(otp!);
                _logger.LogInformation("Register: Account created, verification code sent");
                apiResponse.Status = true;
                apiResponse.Message = "Account created. Enter the code we emailed you to verify your email.";
                apiResponse.ReturnUrl = RouteConstants.VerifyOtp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Register: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region VerifyOtp GET (2-step verification page)
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (!TryGetPendingOtpId(out _))
                return Redirect(RouteConstants.Login);

            ViewBag.Email = HttpContext.Session.GetString(PendingEmailKey);
            return View(new OtpViewModel());
        }
        #endregion

        #region VerifyOtp POST (Step 2: code -> tokens -> signed in)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(OtpViewModel model)
        {
            var apiResponse = new ApiResponse();

            if (!ModelState.IsValid)
                return InvalidInput();

            try
            {
                if (!TryGetPendingOtpId(out var otpId))
                {
                    apiResponse.Status = false;
                    apiResponse.Message = "Session expired. Please login again.";
                    apiResponse.ReturnUrl = RouteConstants.Login;
                    return new ObjectResult(apiResponse);
                }

                var result = await _accountService.VerifyOtpAsync(otpId, model.Otp);

                if (result == null || !result.Status || result.Response == null)
                {
                    apiResponse.Status = false;
                    apiResponse.Message = result?.Message ?? "Verification failed.";
                    return new ObjectResult(apiResponse);
                }

                var token = JsonConvert.DeserializeObject<TokenResponseModel>(result.Response.ToString()!);
                if (token == null || string.IsNullOrEmpty(token.AccessToken))
                {
                    apiResponse.Status = false;
                    apiResponse.Message = "Invalid response from server.";
                    return new ObjectResult(apiResponse);
                }

                // the access token must be in the session before the next API call
                SessionHelper.SetAccessToken(HttpContext.Session, token.AccessToken);
                SessionHelper.SetRefreshToken(HttpContext.Session, token.RefreshToken);
                SessionHelper.SetUserId(HttpContext.Session, token.UserId);
                SessionHelper.SetTenantId(HttpContext.Session, token.TenantId);
                SessionHelper.SetRoleCode(HttpContext.Session, token.RoleCode);
                SessionHelper.SetRoleName(HttpContext.Session, token.RoleName);
                SessionHelper.SetRoleId(HttpContext.Session, token.RoleId);

                var profile = await _userService.GetProfileAsync();
                if (profile != null)
                {
                    SessionHelper.SetTenantId(HttpContext.Session, profile.TenantId);
                    SessionHelper.SetUserFullName(HttpContext.Session, profile.FullName);
                    SessionHelper.SetUserEmail(HttpContext.Session, profile.Email);
                    SessionHelper.SetUserPhone(HttpContext.Session, profile.PhoneNumber ?? "");
                    SessionHelper.SetInitials(HttpContext.Session, profile.Initials);
                    SessionHelper.SetBackgroundColorCode(HttpContext.Session, "#1F3358");
                    SessionHelper.SetColorCode(HttpContext.Session, "#FFFFFF");
                }
                else
                {
                    _logger.LogWarning("VerifyOtp: Profile could not be loaded for user {UserId}", token.UserId);
                }

                CookieHelper.SetRefreshTokenCookie(Response, token.RefreshToken, 7);
                ClearPendingOtp();

                _logger.LogInformation("VerifyOtp: User {UserId} logged in successfully", token.UserId);

                apiResponse.Status = true;
                apiResponse.Message = "Login successful";
                apiResponse.ReturnUrl = RouteConstants.Dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VerifyOtp: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region ResendOtp POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            var apiResponse = new ApiResponse();
            try
            {
                if (!TryGetPendingOtpId(out var otpId))
                {
                    apiResponse.Status = false;
                    apiResponse.Message = "Session expired. Please login again.";
                    apiResponse.ReturnUrl = RouteConstants.Login;
                    return new ObjectResult(apiResponse);
                }

                var result = await _accountService.ResendOtpAsync(otpId);

                if (!TryReadOtpResponse(result, out var otp, apiResponse))
                    return new ObjectResult(apiResponse);

                // the API issues a new OtpId on every resend
                StorePendingOtp(otp!);

                apiResponse.Status = true;
                apiResponse.Message = "A new code was sent to your email.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResendOtp: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region ResetPassword GET (request code + set new password page)
        [HttpGet]
        public IActionResult ResetPassword()
        {
            return View(new ResetPasswordViewModel());
        }
        #endregion

        #region SendResetCode POST (Step 1: email -> code sent)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResetCode(ForgotPasswordViewModel model)
        {
            var apiResponse = new ApiResponse();
            try
            {
                var result = await _accountService.ForgotPasswordAsync(model);
                return new ObjectResult(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendResetCode: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region ResetPassword POST (Step 2: code + new password)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var apiResponse = new ApiResponse();

            try
            {
                var result = await _accountService.ResetPasswordAsync(model);

                apiResponse.Status = result?.Status ?? false;
                apiResponse.Message = result?.Message ?? "Unable to reset the password. Please try again.";

                if (apiResponse.Status)
                {
                    _logger.LogInformation("ResetPassword: Password reset completed");
                    apiResponse.Message = "Password reset successfully. Please sign in.";
                    apiResponse.ReturnUrl = RouteConstants.Login;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ResetPassword: Error occurred");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // revoke the API session first, while the access token is still in the session
            await _accountService.LogoutAsync(RefreshToken);

            _logger.LogInformation("Logout: User {UserId} logged out", CurrentUserId);

            SessionHelper.ClearSession(HttpContext.Session);
            CookieHelper.DeleteRefreshTokenCookie(Response);
            return Redirect(RouteConstants.Login);
        }
        #endregion

        #region AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
        #endregion

        #region Helpers
        private ObjectResult InvalidInput()
        {
            var message = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Invalid input";

            return new ObjectResult(new ApiResponse { Status = false, Message = message });
        }

        // Reads { OtpId, Email, ExpiresInSeconds } from an API reply
        private static bool TryReadOtpResponse(ApiResponse? result, out OtpSentResponseModel? otp, ApiResponse failure)
        {
            otp = null;

            if (result == null || !result.Status || result.Response == null)
            {
                failure.Status = false;
                failure.Message = result?.Message ?? "Request failed. Please try again.";
                return false;
            }

            otp = JsonConvert.DeserializeObject<OtpSentResponseModel>(result.Response.ToString()!);
            if (otp == null || otp.OtpId <= 0)
            {
                failure.Status = false;
                failure.Message = "Invalid response from server.";
                return false;
            }

            return true;
        }

        private void StorePendingOtp(OtpSentResponseModel otp)
        {
            HttpContext.Session.SetString(PendingOtpIdKey, otp.OtpId.ToString());
            HttpContext.Session.SetString(PendingEmailKey, otp.Email ?? "");
        }

        private bool TryGetPendingOtpId(out long otpId)
        {
            return long.TryParse(HttpContext.Session.GetString(PendingOtpIdKey), out otpId) && otpId > 0;
        }

        private void ClearPendingOtp()
        {
            HttpContext.Session.Remove(PendingOtpIdKey);
            HttpContext.Session.Remove(PendingEmailKey);
        }
        #endregion
    }
}