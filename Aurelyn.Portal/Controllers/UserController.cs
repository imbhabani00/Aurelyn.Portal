using Aurelyn.Portal.Models.ChangePasswordViewModel;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Service.User;
using Microsoft.AspNetCore.Mvc;

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
            return PartialView("_ChangePassword", new ChangePasswordViewModel());
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
    }
}