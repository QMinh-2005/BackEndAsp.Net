using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyOwnLearning.Service
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class PermissionAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _module;
        private readonly string _function;
        public PermissionAttribute(string module, string function)
        {
            _module = module;
            _function = function;
        }
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Bạn chưa đăng nhập",
                    success = false
                });
            }
            var requiredPermission = $"{_module}:{_function}";
            var hasPermission = user.Claims
                .Where(c => c.Type == "permission")
                .Any(c => c.Value == requiredPermission);
            if (!hasPermission)
            {
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = $"Bạn không có quyền truy cập chức năng này. Yêu cầu quyền: {requiredPermission}"
                })
                { StatusCode = 403 };
                return;
            }
            await next();
        }
    }
}
