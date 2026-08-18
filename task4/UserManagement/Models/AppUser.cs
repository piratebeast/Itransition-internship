using Microsoft.AspNetCore.Identity;

namespace UserManagement.Models
{
    public enum UserStatus
    {
        Unverified = 0,
        Active = 1,
        Blocked = 2
    }

    public class AppUser : IdentityUser
    {
        public string Name { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public UserStatus Status { get; set; } = UserStatus.Unverified;
    }
}
