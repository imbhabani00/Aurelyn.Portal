using Aurelyn.Portal.Helper;
using Aurelyn.Portal.Models.ChangePasswordViewModel;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Models.User;
using Aurelyn.Portal.Service.User;
using Microsoft.AspNetCore.Mvc;
using ShopEase.WebApp.Helpers;

namespace Aurelyn.App.Controllers
{
    public class UserController : BaseController
    {
        #region Properties
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;
        #endregion

        #region Constructor
        public UserController(
            IUserService userService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }
        #endregion

        #region Profile GET
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            try
            {
                var profile = await _userService.GetProfileAsync();
                if (profile == null)
                    return PartialView("_Profile", null);

                return PartialView("_Profile", profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController.Profile error");
                return PartialView("_Profile", null);
            }
        }
        #endregion

        #region ChangePassword GET
        [HttpGet]
        public IActionResult ChangePassword()
        {
            var data = new ChangePasswordViewModel();
            return PartialView("_ChangePassword", data);
        }
        #endregion

        #region ChangePassword POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var apiResponse = new ApiResponse();

            if (!ModelState.IsValid)
            {
                apiResponse.Status = false;
                apiResponse.Message = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m)) ?? "Invalid input";
                return new ObjectResult(apiResponse);
            }

            try
            {
                apiResponse = await _userService.ChangePasswordAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UserController.ChangePassword error");
                apiResponse.Status = false;
                apiResponse.Message = "An error occurred. Please try again.";
            }

            return new ObjectResult(apiResponse);
        }
        #endregion

        #region UpdateProfile

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileUpdateRequest model)
        {
            var data = new ApiResponse();
            try
            {
                data = await _userService.UpdateProfileAsync(model);
                var profile = await _userService.GetProfileAsync();
                if (profile != null)
                {
                    SessionHelper.SetUserFullName(HttpContext.Session, profile.FullName);
                    SessionHelper.SetUserEmail(HttpContext.Session, profile.Email);
                    SessionHelper.SetUserPhone(HttpContext.Session, profile.PhoneNumber ?? "");
                    SessionHelper.SetInitials(HttpContext.Session, profile.Initials);
                    HttpContext.Session.SetString("ProfileComplete", ProfileHelper.IsComplete(profile) ? "1" : "0");
                    data.Response = profile;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfile error"); data.Status = false; data.Message = "An error occurred. Please try again.";
            }
            return new ObjectResult(data);
        }
        #endregion

        #region UpdateProfile

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile? file)
        {
            var data = new ApiResponse();
            try
            {
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                data = await _userService.UpdateProfilePictureAsync(file);
                if (data.Status)
                {
                    var profile = await _userService.GetProfileAsync();
                    if (profile != null)
                    {
                        HttpContext.Session.SetString("ProfilePicture", profile.ProfilePictureUrl ?? "");
                        data.Response = profile;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateProfilePicture error");
                data.Status = false; data.Message = "An error occurred. Please try again.";
            }
            return new ObjectResult(data);
        }
        #endregion
    }
}