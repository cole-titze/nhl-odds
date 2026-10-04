using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApi.Filters;

/// <summary>
/// Requires an <c>X-Admin-Key</c> header matching the <c>ADMIN_API_KEY</c> setting. Used on every admin action:
/// starting jobs (collection, backfills, predictions) and reading job statuses, health checks and error logs.
/// With no key configured, requests are allowed in Development (local admin page) and rejected elsewhere.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireAdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";
    public const string ConfigKey = "ADMIN_API_KEY";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var expected = services.GetRequiredService<IConfiguration>()[ConfigKey];

        if (string.IsNullOrEmpty(expected))
        {
            if (!services.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
            context.Result = new UnauthorizedResult();
    }
}