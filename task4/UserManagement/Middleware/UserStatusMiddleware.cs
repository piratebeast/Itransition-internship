using Microsoft.AspNetCore.Identity;
using UserManagement.Models;

namespace UserManagement.Middleware;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    // Paths that must stay reachable even for a blocked or deleted account,
    // otherwise a blocked user could never reach the login page to be told why.
    private static readonly string[] AllowedPaths =
    {
        "/Identity/Account/Login",
        "/Identity/Account/Register",
        "/Identity/Account/Logout",
        "/Identity/Account/ConfirmEmail",
        "/Identity/Account/AccessDenied"
    };

    public UserStatusMiddleware(RequestDelegate next) => _next = next;

    // UserManager is scoped, so it is injected per-request here, not in the constructor.
    public async Task InvokeAsync(
        HttpContext context,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        // Anonymous requests have nothing to revalidate; let them through.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        if (AllowedPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Hits the database on every request. The cookie only proves the user
        // authenticated at some point; it says nothing about their status now.
        var user = await userManager.GetUserAsync(context.User);

        if (user is null || user.Status == UserStatus.Blocked)
        {
            await signInManager.SignOutAsync();

            var reason = user is null ? "deleted" : "blocked";
            context.Response.Redirect($"/Identity/Account/Login?reason={reason}");
            return;
        }

        await _next(context);
    }
}

public static class UserStatusMiddlewareExtensions
{
    public static IApplicationBuilder UseUserStatusCheck(this IApplicationBuilder app)
        => app.UseMiddleware<UserStatusMiddleware>();
}