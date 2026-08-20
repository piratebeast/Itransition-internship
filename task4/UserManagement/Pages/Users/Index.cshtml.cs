using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using UserManagement.Data;
using UserManagement.Models;

namespace UserManagement.Pages.Users;

[Authorize]
public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public IndexModel(AppDbContext db, SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _db = db;
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public IList<AppUser> Users { get; set; } = new List<AppUser>();

    [BindProperty]
    public string[] SelectedIds { get; set; } = Array.Empty<string>();

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task OnGetAsync() => await LoadUsersAsync();

    private async Task LoadUsersAsync()
    {
        // Load users from the database, ordered by LastLoginAt (nulls last) and then by LastLoginAt descending.
        Users = await _db.Users
            .AsNoTracking()
            .OrderBy(u => u.LastLoginAt == null)
            .ThenByDescending(u => u.LastLoginAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostBlockAsync()
        => await ApplyStatusAsync(UserStatus.Blocked, "blocked");

    public async Task<IActionResult> OnPostUnblockAsync()
    {
        if (SelectedIds.Length == 0)
        {
            ErrorMessage = "No users selected.";
            return RedirectToPage();
        }

        var affected = await _db.Users
            .Where(u => SelectedIds.Contains(u.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(
                u => u.Status,
                u => u.EmailConfirmed ? UserStatus.Active : UserStatus.Unverified));

        StatusMessage = $"{affected} user(s) unblocked.";
        return RedirectToPage();
    }

    private async Task<IActionResult> ApplyStatusAsync(UserStatus status, string verb)
    {
        if (SelectedIds.Length == 0)
        {
            ErrorMessage = "No users selected.";
            return RedirectToPage();
        }

        // ExecuteUpdateAsync issues a single UPDATE ... WHERE Id = ANY(...)
        // instead of loading every row into memory first.
        var affected = await _db.Users
            .Where(u => SelectedIds.Contains(u.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status));

        StatusMessage = $"{affected} user(s) {verb}.";

        // Blocking yourself must end your own session immediately.
        if (status == UserStatus.Blocked && SelectedIds.Contains(_userManager.GetUserId(User)!))
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login", new { area = "Identity", reason = "blocked" });
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        if (SelectedIds.Length == 0)
        {
            ErrorMessage = "No users selected.";
            return RedirectToPage();
        }

        var selfSelected = SelectedIds.Contains(_userManager.GetUserId(User)!);

        // Spec: deleted users are really deleted, not flagged.
        var affected = await _db.Users
            .Where(u => SelectedIds.Contains(u.Id))
            .ExecuteDeleteAsync();

        StatusMessage = $"{affected} user(s) deleted.";

        if (selfSelected)
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login", new { area = "Identity", reason = "deleted" });
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteUnverifiedAsync()
    {
        var selfId = _userManager.GetUserId(User)!;
        var selfUnverified = await _db.Users
            .AnyAsync(u => u.Id == selfId && u.Status == UserStatus.Unverified);

        var affected = await _db.Users
            .Where(u => u.Status == UserStatus.Unverified)
            .ExecuteDeleteAsync();

        StatusMessage = $"{affected} unverified user(s) deleted.";

        if (selfUnverified)
        {
            await _signInManager.SignOutAsync();
            return RedirectToPage("/Account/Login", new { area = "Identity", reason = "deleted" });
        }

        return RedirectToPage();
    }
}