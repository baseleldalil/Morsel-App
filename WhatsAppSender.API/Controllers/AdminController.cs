using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            SaaSDbContext context,
            ILogger<AdminController> logger)
        {
            _context = context;
            _logger = logger;
        }


        [HttpPost("users")]
        public async Task<ActionResult<UserResponse>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                // Check if user already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new { message = "User with this email already exists" });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    Email = request.Email,
                    FirstName = request.Name,
                    PasswordHash = HashPassword(request.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User created: {Email}", user.Email);

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.FirstName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while creating the user" });
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        [HttpGet("users")]
        public async Task<ActionResult<List<UserResponse>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .OrderByDescending(u => u.CreatedAt)
                    .Select(u => new UserResponse
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Name = u.FirstName,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("users/{id}")]
        public async Task<ActionResult<UserResponse>> GetUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.FirstName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {Id}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the user" });
            }
        }

        /// <summary>
        /// Update user
        /// </summary>
        [HttpPut("users/{id}")]
        public async Task<ActionResult<UserResponse>> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update fields if provided
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    user.FirstName = request.Name;
                }

                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = HashPassword(request.Password);
                }

                if (request.IsActive.HasValue)
                {
                    user.IsActive = request.IsActive.Value;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User updated: {Email}", user.Email);

                return Ok(new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.FirstName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating the user" });
            }
        }

        /// <summary>
        /// Delete user
        /// </summary>
        [HttpDelete("users/{id}")]
        public async Task<ActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User deleted: {Email}", user.Email);

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the user" });
            }
        }

        /// <summary>
        /// Reset user password
        /// </summary>
        [HttpPost("users/{id}/reset-password")]
        public async Task<ActionResult> ResetPassword(string id, [FromBody] ResetPasswordRequest request)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.PasswordHash = HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Password reset for user: {Email}", user.Email);

                return Ok(new { message = "Password reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {Id}", id);
                return StatusCode(500, new { message = "An error occurred while resetting the password" });
            }
        }

        /// <summary>
        /// Assign subscription to user
        /// </summary>
        [HttpPost("subscriptions/assign")]
        public async Task<ActionResult<WhatsApp.Shared.Models.UserSubscription>> AssignSubscription([FromBody] AssignSubscriptionRequest request)
        {
            try
            {
                // Check if subscription exists
                var subscription = await _context.SubscriptionPlans.FindAsync(request.SubscriptionId);
                if (subscription == null)
                {
                    return NotFound(new { message = "Subscription plan not found" });
                }

                // Check if user exists by Email first (more reliable), then by ID
                ApplicationUser? user = null;

                // First try to find by email
                if (!string.IsNullOrWhiteSpace(request.UserEmail))
                {
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.UserEmail);
                }

                // If not found by email and UserId is provided, try by ID
                if (user == null && request.UserId > 0)
                {
                    user = await _context.Users.FindAsync(request.UserId.ToString());
                }

                if (user == null)
                {
                    // Create new user if not found
                    if (string.IsNullOrWhiteSpace(request.Password))
                    {
                        return BadRequest(new { message = "Password is required when creating a new user" });
                    }

                    user = new ApplicationUser
                    {
                        FirstName = request.UserEmail,
                        Email = request.UserEmail,
                        PasswordHash = HashPassword(request.Password),
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New user created: {Email}", user.Email);
                }

                // Update user password if provided
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    user.PasswordHash = HashPassword(request.Password);
                }

                // Update user email if it doesn't match
                if (user.Email != request.UserEmail)
                {
                    user.Email = request.UserEmail;
                }

                // Check if user already has this subscription
                var existingAssignment = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(us => us.UserId == user.Id && us.SubscriptionPlanId == request.SubscriptionId && us.IsActive);

                WhatsApp.Shared.Models.UserSubscription userSubscription;
                if (existingAssignment != null)
                {
                    // Update existing assignment
                    existingAssignment.IsActive = request.IsActive;
                    userSubscription = existingAssignment;
                }
                else
                {
                    // Create new assignment
                    userSubscription = new WhatsApp.Shared.Models.UserSubscription
                    {
                        SubscriptionPlanId = request.SubscriptionId,
                        UserId = user.Id,
                        IsActive = request.IsActive,
                    };
                    _context.UserSubscriptions.Add(userSubscription);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription {SubscriptionName} assigned to user {Email}",
                    subscription.Name, user.Email);

                return Ok(new WhatsApp.Shared.Models.UserSubscription
                {
                    Id = userSubscription.Id,
                    UserId = user.Id,
                    
                    IsActive = userSubscription.IsActive,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning subscription to user {UserEmail}. Request: {@Request}", request.UserEmail, request);
                return StatusCode(500, new { message = "An error occurred while assigning the subscription", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all user subscription assignments
        /// </summary>
        [HttpGet("subscriptions/assignments")]
        public async Task<ActionResult<List<UserSubscriptionResponse>>> GetAllSubscriptionAssignments()
        {
            try
            {
                var dbAssignments = await _context.UserSubscriptions
                    .Include(us => us.User)
                    .Include(us => us.SubscriptionPlan)
                    .OrderByDescending(us => us.StartDate)
                    .ToListAsync();

                var assignments = dbAssignments.Select(us => new UserSubscriptionResponse
                {
                    Id = us.Id,
                    UserId = us.User?.Id ?? us.UserId,
                    UserEmail = us.User?.Email ?? us.UserId,
                    UserName = us.User?.FirstName,
                    SubscriptionId = us.SubscriptionPlanId,
                    SubscriptionName = us.SubscriptionPlan?.Name ?? string.Empty,
                    IsActive = us.IsActive
                }).ToList();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription assignments");
                return StatusCode(500, new { message = "An error occurred while retrieving assignments" });
            }
        }

        /// <summary>
        /// Get user's subscription assignments
        /// </summary>
        [HttpGet("subscriptions/assignments/user/{userId}")]
        public async Task<ActionResult<List<UserSubscriptionResponse>>> GetUserSubscriptionAssignments(string userId)
        {
            try
            {
                // Find shared user by string id
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var dbAssignments = await _context.UserSubscriptions
                    .Include(us => us.SubscriptionPlan)
                    .Where(us => us.UserId == user.Id)
                    .OrderByDescending(us => us.StartDate)
                    .ToListAsync();

                var assignments = dbAssignments.Select(us => new UserSubscriptionResponse
                {
                    Id = us.Id,
                    UserId = user.Id,
                    UserEmail = user.Email,
                    UserName = user.FirstName,
                    SubscriptionId = us.SubscriptionPlanId,
                    SubscriptionName = us.SubscriptionPlan?.Name ?? string.Empty,
                    IsActive = us.IsActive,
                    AssignedAt = us.CreatedAt
                }).ToList();

                return Ok(assignments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user subscription assignments for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving user assignments" });
            }
        }

        /// <summary>
        /// Get all subscription plans
        /// </summary>
        [HttpGet("subscriptions")]
        public async Task<ActionResult<List<Subscription>>> GetSubscriptions()
        {
            try
            {
                var subscriptions = await _context.SubscriptionPlans
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Price)
                    .ToListAsync();

                return Ok(subscriptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscriptions");
                return StatusCode(500, new { message = "An error occurred while retrieving subscriptions" });
            }
        }

        /// <summary>
        /// Revoke user subscription assignment
        /// </summary>
        [HttpDelete("subscriptions/assignments/{assignmentId}")]
        public async Task<ActionResult> RevokeSubscriptionAssignment(int assignmentId)
        {
            try
            {
                var assignment = await _context.UserSubscriptions.FindAsync(assignmentId);

                if (assignment == null)
                {
                    return NotFound(new { message = "Assignment not found" });
                }

                assignment.IsActive = false;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Subscription assignment {AssignmentId} revoked", assignmentId);

                return Ok(new { message = "Subscription assignment revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking subscription assignment {AssignmentId}", assignmentId);
                return StatusCode(500, new { message = "An error occurred while revoking the assignment" });
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hash);
        }
    }

    public class ResetPasswordRequest
    {
        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
