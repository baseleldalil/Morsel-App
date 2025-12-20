using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppAdmin.Models;
using WhatsAppAdmin.Services;

namespace WhatsAppAdmin.Controllers
{
    /// <summary>
    /// Controller for managing campaign templates
    /// Handles CRUD operations for reusable message templates
    /// </summary>
    [Authorize(Roles = "SuperAdmin,Admin,SemiAdmin")]
    public class CampaignTemplatesController : Controller
    {
        private readonly ICampaignTemplateService _campaignTemplateService;
        private readonly IFileUploadService _fileUploadService;

        public CampaignTemplatesController(ICampaignTemplateService campaignTemplateService, IFileUploadService fileUploadService)
        {
            _campaignTemplateService = campaignTemplateService;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// Display list of all campaign templates
        /// </summary>
        public async Task<IActionResult> Index(string? category = null)
        {
            IEnumerable<CampaignTemplate> templates;

            if (string.IsNullOrEmpty(category))
            {
                templates = await _campaignTemplateService.GetAllTemplatesAsync();
            }
            else
            {
                templates = await _campaignTemplateService.GetTemplatesByCategoryAsync(category);
            }

            ViewBag.Categories = await _campaignTemplateService.GetCategoriesAsync();
            ViewBag.SelectedCategory = category;
            return View(templates);
        }

        /// <summary>
        /// Display template details
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var template = await _campaignTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound();

            return View(template);
        }

        /// <summary>
        /// Display form to create new template
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _campaignTemplateService.GetCategoriesAsync();
            return View();
        }

        /// <summary>
        /// Process template creation
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Create(CampaignTemplate template, IList<IFormFile> attachments)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _campaignTemplateService.CreateTemplateAsync(template);

                    // Handle file uploads if any
                    if (attachments != null && attachments.Any())
                    {
                        var uploadedFiles = await _fileUploadService.ProcessUploadsAsync(attachments, template.Id);
                        // Note: In a real implementation, you'd save these to the database through a repository
                    }

                    TempData["Success"] = "Campaign template created successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating template: {ex.Message}");
                }
            }

            ViewBag.Categories = await _campaignTemplateService.GetCategoriesAsync();
            return View(template);
        }

        /// <summary>
        /// Display form to edit template
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var template = await _campaignTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound();

            ViewBag.Categories = await _campaignTemplateService.GetCategoriesAsync();
            return View(template);
        }

        /// <summary>
        /// Process template update
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Edit(int id, CampaignTemplate template)
        {
            if (id != template.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _campaignTemplateService.UpdateTemplateAsync(template);
                    TempData["Success"] = "Campaign template updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating template: {ex.Message}");
                }
            }

            ViewBag.Categories = await _campaignTemplateService.GetCategoriesAsync();
            return View(template);
        }

        /// <summary>
        /// Display confirmation page for template deletion
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var template = await _campaignTemplateService.GetTemplateByIdAsync(id);
            if (template == null)
                return NotFound();

            return View(template);
        }

        /// <summary>
        /// Process template deletion
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _campaignTemplateService.DeleteTemplateAsync(id);
                if (result)
                {
                    TempData["Success"] = "Campaign template deleted successfully.";
                }
                else
                {
                    TempData["Error"] = "Template not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting template: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Toggle template active status via AJAX
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                var template = await _campaignTemplateService.GetTemplateByIdAsync(id);
                if (template == null)
                    return Json(new { success = false, message = "Template not found" });

                template.IsActive = !template.IsActive;
                await _campaignTemplateService.UpdateTemplateAsync(template);

                return Json(new { success = true, isActive = template.IsActive });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}