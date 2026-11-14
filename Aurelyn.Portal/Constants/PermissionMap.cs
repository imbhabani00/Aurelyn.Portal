using Aurelyn.Portal.Models.Common;

namespace Aurelyn.Portal.Constants
{
    public class PermissionMap
    {
        public static readonly Dictionary<string, (string ModuleCode, Func<PermissionModel, bool> Selector)> Map = new()
        {
            { PermissionConstants.Dashboard_View, ("DASHBOARD", m => m.CanView) },
        };
    }
}
