using Aurelyn.Portal.Constants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ShopEase.WebApp.Helpers;

namespace Aurelyn.App.Controllers
{
    public class BaseController : Controller
    {
        #region Session Properties
        protected string? AccessToken => HttpContext.Session.GetString(SessionConstants.AccessToken);
        protected string? RefreshToken => HttpContext.Session.GetString(SessionConstants.RefreshToken);
        protected int? CurrentUserId => HttpContext.Session.GetInt32(SessionConstants.UserId);
        protected int? CurrentTenantId => HttpContext.Session.GetInt32(SessionConstants.TenantId);
        protected string? CurrentRoleCode => HttpContext.Session.GetString(SessionConstants.RoleCode);
        protected string? CurrentRoleName => HttpContext.Session.GetString(SessionConstants.RoleName);
        protected string? CurrentUserFullName => HttpContext.Session.GetString(SessionConstants.UserFullName);
        protected string? CurrentUserEmail => HttpContext.Session.GetString(SessionConstants.UserEmail);
        protected string? CurrentUserPhone => HttpContext.Session.GetString(SessionConstants.UserPhone);
        protected string? CurrentInitials => HttpContext.Session.GetString(SessionConstants.Initials);
        protected string? CurrentBackgroundColorCode => HttpContext.Session.GetString(SessionConstants.BackgroundColorCode);
        protected string? CurrentProfilePicturePath => HttpContext.Session.GetString(SessionConstants.ProfilePicturePath);
        #endregion

        #region Permission Helpers
        protected bool HasPermission(string flatPermissionKey)
            => SessionHelper.HasPermission(HttpContext.Session, flatPermissionKey);
        #endregion

        #region TempData Helpers
        protected void SetSuccessMessage(string message)
            => TempData["SuccessMessage"] = message;

        protected void SetErrorMessage(string message)
            => TempData["ErrorMessage"] = message;
        #endregion

        #region OnActionExecuting
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                ViewBag.Permissions = SessionHelper.BuildPermissionViewModel(HttpContext.Session);

                ViewData["UserFullName"] = CurrentUserFullName;
                ViewData["RoleName"] = CurrentRoleName;
                ViewData["UserInitials"] = CurrentInitials;
                ViewData["BackgroundColorCode"] = CurrentBackgroundColorCode;
                ViewData["ProfilePicturePath"] = CurrentProfilePicturePath;
            }

            base.OnActionExecuting(context);
        }
        #endregion
    }
}