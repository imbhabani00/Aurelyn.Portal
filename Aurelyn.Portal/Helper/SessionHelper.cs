using Aurelyn.Portal.Constants;
using Aurelyn.Portal.Models.Common;
using Aurelyn.Portal.Models.Permission;
using Newtonsoft.Json;

namespace ShopEase.WebApp.Helpers
{
    public class SessionHelper
    {
        #region Setters
        public static void SetAccessToken(ISession session, string? token)
            => session.SetString(SessionConstants.AccessToken, token ?? string.Empty);

        public static void SetRefreshToken(ISession session, string? token)
            => session.SetString(SessionConstants.RefreshToken, token ?? string.Empty);

        public static void SetUserId(ISession session, int userId)
            => session.SetInt32(SessionConstants.UserId, userId);

        public static void SetRoleCode(ISession session, string? roleCode)
            => session.SetString(SessionConstants.RoleCode, roleCode ?? string.Empty);

        public static void SetRoleId(ISession session, int roleId)
            => session.SetInt32(SessionConstants.RoleId, roleId);

        public static void SetRoleName(ISession session, string? roleName)
            => session.SetString(SessionConstants.RoleName, roleName ?? string.Empty);

        public static void SetTenantId(ISession session, int tenantId)
            => session.SetInt32(SessionConstants.TenantId, tenantId);

        public static void SetUserFullName(ISession session, string? fullName)
            => session.SetString(SessionConstants.UserFullName, fullName ?? string.Empty);

        public static void SetUserEmail(ISession session, string? email)
            => session.SetString(SessionConstants.UserEmail, email ?? string.Empty);

        public static void SetUserPhone(ISession session, string? phone)
            => session.SetString(SessionConstants.UserPhone, phone ?? string.Empty);

        public static void SetInitials(ISession session, string? initials)
            => session.SetString(SessionConstants.Initials, initials ?? string.Empty);

        public static void SetBackgroundColorCode(ISession session, string? color)
            => session.SetString(SessionConstants.BackgroundColorCode, color ?? string.Empty);

        public static void SetColorCode(ISession session, string? color)
            => session.SetString(SessionConstants.ColorCode, color ?? string.Empty);
        #endregion

        #region Getters
        public static string? GetAccessToken(ISession session)
            => session.GetString(SessionConstants.AccessToken);

        public static string? GetRefreshToken(ISession session)
            => session.GetString(SessionConstants.RefreshToken);

        #endregion

        #region Permissions
        public static void SetPermissions(ISession session, List<PermissionModel> permissions)
            => session.SetString(SessionConstants.Permissions,
                JsonConvert.SerializeObject(permissions ?? new List<PermissionModel>()));

        public static List<PermissionModel> GetPermissions(ISession session)
        {
            var json = session.GetString(SessionConstants.Permissions);
            if (string.IsNullOrEmpty(json)) return new List<PermissionModel>();
            return JsonConvert.DeserializeObject<List<PermissionModel>>(json)
                   ?? new List<PermissionModel>();
        }

        public static bool HasPermission(ISession session, string flatPermissionKey)
        {
            if (!PermissionMap.Map.TryGetValue(flatPermissionKey, out var mapping))
                return false;

            var permissions = GetPermissions(session);
            var module = permissions.FirstOrDefault(p =>
                p.ModuleCode.Equals(mapping.ModuleCode, StringComparison.OrdinalIgnoreCase));

            if (module == null) return false;

            return mapping.Selector(module);
        }

        public static PermissionViewModel BuildPermissionViewModel(ISession session)
        {
            var vm = new PermissionViewModel();
            foreach (var key in PermissionMap.Map.Keys)
            {
                vm.Permissions[key] = HasPermission(session, key);
            }
            return vm;
        }
        #endregion

        #region Clear
        public static void ClearSession(ISession session)
            => session.Clear();
        #endregion
    }
}