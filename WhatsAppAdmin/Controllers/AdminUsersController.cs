using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using WhatsApp.Shared.Models;
using WhatsApp.Shared.Data;

namespace WhatsAppAdmin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SaaSDbContext _saasContext;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SaaSDbContext saasContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _saasContext = saasContext;
        }

        // GET: AdminUsers
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<AdminUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = roles.ToList()
                });
            }

            return View(userViewModels);
        }

        // GET: AdminUsers/Create
        [Authorize(Roles = "SuperAdmin")]
        public IActionResult Create()
        {
            ViewBag.Roles = new List<string> { "Admin", "SemiAdmin" };
            return View();
        }

        // POST: AdminUsers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Create(CreateAdminUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    TempData["Success"] = "Admin user created successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new List<string> { "Admin", "SemiAdmin" };
            return View(model);
        }

        // GET: AdminUsers/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var model = new EditAdminUserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CurrentRole = userRoles.FirstOrDefault() ?? string.Empty
            };

            ViewBag.Roles = new List<string> { "Admin", "SemiAdmin" };
            return View(model);
        }

        // POST: AdminUsers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditAdminUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.IsActive = model.IsActive;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    // Update role if changed
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                    {
                        await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    }

                    if (!string.IsNullOrEmpty(model.NewRole))
                    {
                        await _userManager.AddToRoleAsync(user, model.NewRole);
                    }

                    TempData["Success"] = "Admin user updated successfully!";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            ViewBag.Roles = new List<string> { "Admin", "SemiAdmin" };
            return View(model);
        }

        // POST: AdminUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                // Don't allow deleting SuperAdmin users
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Contains("SuperAdmin"))
                {
                    await _userManager.DeleteAsync(user);
                    TempData["Success"] = "Admin user deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Cannot delete SuperAdmin users!";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateAdminUserViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;
    }

    public class EditAdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public string CurrentRole { get; set; } = string.Empty;

        public string NewRole { get; set; } = string.Empty;
    }
}