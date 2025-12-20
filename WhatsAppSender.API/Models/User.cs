using System.ComponentModel.DataAnnotations;

namespace WhatsAppSender.API.Models
{
    public class User
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Name { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsAdmin { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }

    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public UserInfo? User { get; set; }
    }

    public class UserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? LastLoginAt { get; set; }
    }

    public class CreateUserRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public string? Name { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? Name { get; set; }
        public string? Password { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
