using BilliardsManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BilliardsManagement.Attributes
{
    public class RequirePermissionAttribute : ActionFilterAttribute
    {
        private readonly string[] _permissionNames;
        private readonly bool _requireAll;

        public RequirePermissionAttribute(params string[] permissionNames)
        {
            _permissionNames = permissionNames;
            _requireAll = false;
        }

        public RequirePermissionAttribute(bool requireAll, params string[] permissionNames)
        {
            _permissionNames = permissionNames;
            _requireAll = requireAll;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var permissionService = httpContext.RequestServices.GetService<IPermissionService>();

            if (permissionService == null)
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Check if user is logged in
            var employeeId = httpContext.Session.GetInt32("EmployeeId");
            var role = httpContext.Session.GetString("Role");

            if (!employeeId.HasValue || string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Managers always have access
            if (role.ToUpper() == "MANAGER")
            {
                await next();
                return;
            }

            // Check permissions
            bool hasPermission;
            if (_requireAll)
            {
                // User must have ALL specified permissions
                hasPermission = true;
                foreach (var permission in _permissionNames)
                {
                    if (!await permissionService.HasPermissionAsync(employeeId.Value, permission))
                    {
                        hasPermission = false;
                        break;
                    }
                }
            }
            else
            {
                // User must have ANY of the specified permissions
                hasPermission = await permissionService.HasAnyPermissionAsync(employeeId.Value, _permissionNames);
            }

            if (!hasPermission)
            {
                // Set error message
                httpContext.Session.SetString("TempError", "Bạn không có quyền truy cập chức năng này. Vui lòng liên hệ quản lý để được cấp quyền.");
                
                // Redirect to appropriate dashboard based on role
                var redirectAction = role.ToUpper() switch
                {
                    "CASHIER" => new RedirectToActionResult("Index", "Cashier", null),
                    "SERVING" => new RedirectToActionResult("Index", "Serving", null),
                    _ => new RedirectToActionResult("Index", "Home", null)
                };
                
                context.Result = redirectAction;
                return;
            }

            await next();
        }
    }
} 