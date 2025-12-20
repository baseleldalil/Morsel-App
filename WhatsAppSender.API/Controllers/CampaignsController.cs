using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WhatsAppSender.API.Services;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Models;
using WhatsAppSender.API.Models;

namespace WhatsAppSender.API.Controllers
{
    /// <summary>
    /// Campaign workflow management API - Start, Pause, Stop, and monitor campaigns
    /// </summary>
    /// <remarks>
    /// Campaign Status Flow:
    /// - Pending -> Running -> Paused -> Running -> Completed
    /// - Running -> Stopped (cannot restart)
    ///
    /// All endpoints require X-API-Key header for authentication.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Tags("Campaigns")]
    public class CampaignsController : ControllerBase
    {
        private readonly SaaSDbContext _context;
        private readonly IApiKeyService _apiKeyService;
        private readonly ICampaignExecutorService _campaignExecutorService;
        private readonly ITemplateValidationService _templateValidationService;
        private readonly ILogger<CampaignsController> _logger;

        public CampaignsController(
            SaaSDbContext context,
            IApiKeyService apiKeyService,
            ICampaignExecutorService campaignExecutorService,
            ITemplateValidationService templateValidationService,
            ILogger<CampaignsController> logger)
        {
            _context = context;
            _apiKeyService = apiKeyService;
            _campaignExecutorService = campaignExecutorService;
            _templateValidationService = templateValidationService;
            _logger = logger;
        }

        /// <summary>
        /// Get all campaigns for the authenticated user
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
        /// <returns>Paginated list of campaigns</returns>
        /// <response code="200">Returns the list of campaigns</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(CampaignListResponse), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCampaigns([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Get campaigns
                var query = _context.Campaigns
                    .AsNoTracking()
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedAt);

                var totalCount = await query.CountAsync();
                var campaigns = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CampaignResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        UserId = c.UserId,
                        CampaignTemplateId = c.CampaignTemplateId,
                        Status = c.Status.ToString(),
                        TotalContacts = c.TotalContacts,
                        MessagesSent = c.MessagesSent,
                        MessagesDelivered = c.MessagesDelivered,
                        MessagesFailed = c.MessagesFailed,
                        CurrentProgress = c.CurrentProgress,
                        CreatedAt = c.CreatedAt,
                        StartedAt = c.StartedAt,
                        PausedAt = c.PausedAt,
                        StoppedAt = c.StoppedAt,
                        CompletedAt = c.CompletedAt,
                        UpdatedAt = c.UpdatedAt,
                        LastError = c.LastError,
                        ErrorCount = c.ErrorCount,
                        MessageContent = c.MessageContent,
                        UseGenderTemplates = c.UseGenderTemplates,
                        MaleContent = c.MaleContent,
                        FemaleContent = c.FemaleContent,
                        ProgressPercentage = c.TotalContacts > 0
                            ? (decimal)c.CurrentProgress / c.TotalContacts * 100
                            : 0
                    })
                    .ToListAsync();

                return Ok(new CampaignListResponse
                {
                    Campaigns = campaigns,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaigns");
                return StatusCode(500, new { error = "Internal server error while retrieving campaigns" });
            }
        }

        /// <summary>
        /// Get a specific campaign by ID
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Campaign details</returns>
        /// <response code="200">Returns the campaign</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CampaignResponse), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCampaign(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var campaign = await _context.Campaigns
                    .AsNoTracking()
                    .Where(c => c.Id == id && c.UserId == userId)
                    .Select(c => new CampaignResponse
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        UserId = c.UserId,
                        CampaignTemplateId = c.CampaignTemplateId,
                        Status = c.Status.ToString(),
                        TotalContacts = c.TotalContacts,
                        MessagesSent = c.MessagesSent,
                        MessagesDelivered = c.MessagesDelivered,
                        MessagesFailed = c.MessagesFailed,
                        CurrentProgress = c.CurrentProgress,
                        CreatedAt = c.CreatedAt,
                        StartedAt = c.StartedAt,
                        PausedAt = c.PausedAt,
                        StoppedAt = c.StoppedAt,
                        CompletedAt = c.CompletedAt,
                        UpdatedAt = c.UpdatedAt,
                        LastError = c.LastError,
                        ErrorCount = c.ErrorCount,
                        MessageContent = c.MessageContent,
                        UseGenderTemplates = c.UseGenderTemplates,
                        MaleContent = c.MaleContent,
                        FemaleContent = c.FemaleContent,
                        ProgressPercentage = c.TotalContacts > 0
                            ? (decimal)c.CurrentProgress / c.TotalContacts * 100
                            : 0
                    })
                    .FirstOrDefaultAsync();

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                return Ok(campaign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving campaign");
                return StatusCode(500, new { error = "Internal server error while retrieving campaign" });
            }
        }

        /// <summary>
        /// Create a new campaign
        /// </summary>
        /// <param name="request">Campaign creation data</param>
        /// <returns>The created campaign</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /api/campaigns
        ///     {
        ///         "name": "My Campaign",
        ///         "description": "Campaign description",
        ///         "messageContent": "Hello {Name}!",
        ///         "totalContacts": 100,
        ///         "useGenderTemplates": false
        ///     }
        ///
        /// </remarks>
        /// <response code="201">Campaign created successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(CampaignResponse), 201)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Create campaign with transaction for data consistency
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var campaign = new Campaign
                    {
                        Name = request.Name,
                        Description = request.Description ?? $"Campaign with {request.TotalContacts} contacts",
                        UserId = userId,
                        CampaignTemplateId = request.CampaignTemplateId,
                        MessageContent = request.MessageContent,
                        UseGenderTemplates = request.UseGenderTemplates,
                        MaleContent = request.MaleContent,
                        FemaleContent = request.FemaleContent,
                        TotalContacts = request.TotalContacts,
                        Status = CampaignStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Campaigns.Add(campaign);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Campaign created: {campaign.Name} (ID: {campaign.Id}) for user {userId}");

                    var response = new CampaignResponse
                    {
                        Id = campaign.Id,
                        Name = campaign.Name,
                        Description = campaign.Description,
                        UserId = campaign.UserId,
                        CampaignTemplateId = campaign.CampaignTemplateId,
                        Status = campaign.Status.ToString(),
                        TotalContacts = campaign.TotalContacts,
                        MessagesSent = campaign.MessagesSent,
                        MessagesDelivered = campaign.MessagesDelivered,
                        MessagesFailed = campaign.MessagesFailed,
                        CurrentProgress = campaign.CurrentProgress,
                        CreatedAt = campaign.CreatedAt,
                        MessageContent = campaign.MessageContent,
                        UseGenderTemplates = campaign.UseGenderTemplates,
                        MaleContent = campaign.MaleContent,
                        FemaleContent = campaign.FemaleContent,
                        ProgressPercentage = 0
                    };

                    return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, response);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign");
                return StatusCode(500, new { error = "Internal server error while creating campaign" });
            }
        }

        /// <summary>
        /// Start a campaign or resume a paused campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="request">Optional timing and browser settings</param>
        /// <returns>Start result with timing settings</returns>
        /// <remarks>
        /// Start modes:
        /// - **Auto timing**: Uses database timing settings (default)
        /// - **Manual timing**: Custom min/max delay in seconds
        ///
        /// Browser types: chrome (default), firefox
        ///
        /// Sample request:
        ///
        ///     POST /api/campaigns/1/start
        ///     {
        ///         "timingMode": "manual",
        ///         "manualTiming": {
        ///             "minDelay": 30,
        ///             "maxDelay": 60
        ///         },
        ///         "browserType": "chrome"
        ///     }
        ///
        /// Status restrictions:
        /// - Cannot start if already Running
        /// - Cannot start if Stopped (create new campaign instead)
        /// - Cannot start if Completed
        /// - CAN start if Paused (resumes campaign)
        /// </remarks>
        /// <response code="200">Campaign started successfully</response>
        /// <response code="400">Campaign cannot be started (already running, stopped, or completed)</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Failed to start campaign</response>
        [HttpPost("{id}/start")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> StartCampaign(int id, [FromBody] StartCampaignRequest? request = null)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Check if campaign can be started
                if (campaign.Status == CampaignStatus.Running)
                {
                    return BadRequest(new { error = "Campaign is already running" });
                }

                if (campaign.Status == CampaignStatus.Stopped)
                {
                    return BadRequest(new { error = "Stopped campaigns cannot be restarted. Create a new campaign instead." });
                }

                if (campaign.Status == CampaignStatus.Completed)
                {
                    return BadRequest(new { error = "Campaign has already completed" });
                }

                // Validate message template before starting campaign
                if (!string.IsNullOrWhiteSpace(campaign.MessageContent))
                {
                    // Get contacts for this campaign to validate template against dataset
                    var campaignContacts = await _context.Contacts
                        .Where(c => c.CampaignId == id)
                        .Take(1)
                        .ToListAsync();

                    if (campaignContacts.Any())
                    {
                        var validationResult = _templateValidationService.ValidateTemplateForCampaign(
                            campaign.MessageContent,
                            campaignContacts);

                        if (!validationResult.IsValid)
                        {
                            return BadRequest(new
                            {
                                error = "Template validation failed",
                                validation_errors = validationResult.Errors,
                                variables_found = validationResult.Variables,
                                message = validationResult.GetErrorMessage()
                            });
                        }

                        _logger.LogInformation("Template validated successfully for campaign {CampaignId}. Variables: {Variables}",
                            id, string.Join(", ", validationResult.Variables));
                    }
                }

                // Check if there are pending contacts for this user
                var pendingContacts = await _context.Contacts
                    .Where(c => c.UserId == userId && c.Status == ContactStatus.Pending && c.IsSelected == true)
                    .ToListAsync();

                if (pendingContacts.Count == 0)
                {
                    return BadRequest(new { error = "No pending contacts found for this user" });
                }

                // Link pending contacts to this campaign
                foreach (var contact in pendingContacts)
                {
                    contact.CampaignId = id;
                    contact.UpdatedAt = DateTime.UtcNow;
                }

                // Update campaign total contacts
                campaign.TotalContacts = pendingContacts.Count;
                campaign.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Linked {pendingContacts.Count} pending contacts to campaign {id}");

                var pendingCount = pendingContacts.Count;

                // Parse timing settings
                var timingMode = request?.TimingMode ?? "auto";
                Services.TimingMode mode = timingMode.ToLower() == "manual"
                    ? Services.TimingMode.Manual
                    : Services.TimingMode.Auto;

                Services.ManualTimingSettings? manualSettings = null;
                if (mode == Services.TimingMode.Manual && request?.ManualTiming != null)
                {
                    manualSettings = new Services.ManualTimingSettings
                    {
                        MinDelay = request.ManualTiming.MinDelay > 0 ? request.ManualTiming.MinDelay : 30,
                        MaxDelay = request.ManualTiming.MaxDelay > 0 ? request.ManualTiming.MaxDelay : 60
                    };
                }

                // Parse browser type
                var browserTypeStr = request?.BrowserType ?? "chrome";
                Services.BrowserType browserType = browserTypeStr.ToLower() == "firefox"
                    ? Services.BrowserType.Firefox
                    : Services.BrowserType.Chrome;

                // Start campaign using executor service
                var started = await _campaignExecutorService.StartCampaignAsync(
                    id,
                    apiKey,
                    mode,
                    manualSettings,
                    browserType);

                if (!started)
                {
                    return StatusCode(500, new { error = "Failed to start campaign execution" });
                }

                _logger.LogInformation($"Campaign started: {campaign.Name} (ID: {campaign.Id}), Mode: {mode}");

                var response = new
                {
                    message = "Campaign started successfully",
                    status = "Running",
                    timing_mode = mode.ToString(),
                    timing_settings = (object)(mode == Services.TimingMode.Manual
                        ? new { mode = "manual", min_delay = manualSettings?.MinDelay ?? 30, max_delay = manualSettings?.MaxDelay ?? 60 }
                        : new { mode = "auto", min_delay = (int?)null, max_delay = (int?)null, message = "Using database timing settings" }),
                    pending_contacts = pendingCount
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting campaign {id}");
                return StatusCode(500, new { error = "Internal server error while starting campaign" });
            }
        }

        /// <summary>
        /// Pause a running campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="request">Optional progress data to save</param>
        /// <returns>Pause result with current progress</returns>
        /// <response code="200">Campaign paused successfully</response>
        /// <response code="400">Campaign is not running</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Failed to pause campaign</response>
        [HttpPost("{id}/pause")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> PauseCampaign(int id, [FromBody] CampaignProgressRequest? request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Check if campaign can be paused
                if (campaign.Status != CampaignStatus.Running)
                {
                    return BadRequest(new { error = "Only running campaigns can be paused" });
                }

                // Use transaction to ensure atomicity - only save progress if pause succeeds
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Save current progress if explicitly provided (must be > 0 or explicitly set)
                    // FIX: Changed from >= 0 to explicit null check and > 0 to avoid always saving 0
                    if (request != null && request.CurrentProgress > 0)
                    {
                        campaign.CurrentProgress = request.CurrentProgress;
                        campaign.UpdatedAt = DateTime.UtcNow;
                        _logger.LogInformation($"Campaign progress will be saved: {campaign.CurrentProgress} contacts processed");
                    }

                    // Pause campaign using executor service
                    var paused = await _campaignExecutorService.PauseCampaignAsync(id);

                    if (!paused)
                    {
                        // Rollback any progress changes if pause failed
                        await transaction.RollbackAsync();
                        return StatusCode(500, new { error = "Failed to pause campaign" });
                    }

                    // Only save to database after executor confirms pause
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Campaign paused: {campaign.Name} (ID: {campaign.Id}) at contact {campaign.CurrentProgress}");

                    return Ok(new {
                        message = "Campaign paused successfully",
                        status = "Paused",
                        currentProgress = campaign.CurrentProgress
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pausing campaign {id}");
                return StatusCode(500, new { error = "Internal server error while pausing campaign" });
            }
        }

        /// <summary>
        /// Stop a campaign permanently (cannot be restarted)
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="request">Optional progress data to save</param>
        /// <returns>Stop result</returns>
        /// <remarks>
        /// This will:
        /// - Cancel campaign execution
        /// - Close all browser sessions
        /// - Kill all Chrome/ChromeDriver processes
        /// - Mark campaign as Stopped
        ///
        /// **Warning**: Stopped campaigns cannot be restarted. Create a new campaign instead.
        ///
        /// Sample request:
        ///
        ///     POST /api/campaigns/1/stop
        ///     {
        ///         "currentProgress": 50
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Campaign stopped successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Failed to stop campaign</response>
        [HttpPost("{id}/stop")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> StopCampaign(int id, [FromBody] CampaignProgressRequest? request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Save current progress if provided (last contact reached before stop)
                if (request?.CurrentProgress >= 0)
                {
                    campaign.CurrentProgress = request.CurrentProgress;
                    campaign.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Campaign stopped progress saved: {campaign.CurrentProgress} contacts processed");
                }

                // Stop campaign using executor service
                // This will cancel the execution, close browser session, and update status
                var stopped = await _campaignExecutorService.StopCampaignAsync(id);

                if (!stopped)
                {
                    // Campaign may not be running, so just update status in database with transaction
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        campaign.Status = CampaignStatus.Stopped;
                        campaign.StoppedAt = DateTime.UtcNow;
                        campaign.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // ALWAYS close browser session for this user, regardless of campaign state
                // This ensures Chrome closes even if campaign wasn't tracked as running
                int totalKilled = 0;
                try
                {
                    // Get user email - try multiple sources
                    var userEmail = apiKeyEntity.UserEmail;

                    // If UserEmail is null, load from database
                    if (string.IsNullOrEmpty(userEmail))
                    {
                        var user = await _context.Users.FindAsync(userId);
                        if (user != null)
                        {
                            userEmail = user.Email;
                            _logger.LogInformation($"Retrieved user email from database: {userEmail}");
                        }
                    }

                    // ========== ABSOLUTE NUCLEAR OPTION: KILL ALL CHROME IMMEDIATELY ==========
                    _logger.LogWarning("🔥🔥🔥 ABSOLUTE NUCLEAR: KILLING ALL CHROME PROCESSES NOW!");

                    try
                    {
                        // Method 1: PowerShell - Most reliable
                        _logger.LogInformation("Executing PowerShell: Stop-Process chrome, chromedriver");
                        var ps = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "powershell",
                            Arguments = "-Command \"Get-Process chrome,chromedriver -ErrorAction SilentlyContinue | Stop-Process -Force\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var psProc = System.Diagnostics.Process.Start(ps);
                        psProc?.WaitForExit(5000);
                        _logger.LogInformation("✓ PowerShell kill executed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "PowerShell kill failed");
                    }

                    await Task.Delay(500);

                    try
                    {
                        // Method 2: taskkill - Backup
                        _logger.LogInformation("Executing taskkill for chrome.exe");
                        var tk1 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = "/F /IM chrome.exe /T",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var tk1Proc = System.Diagnostics.Process.Start(tk1);
                        tk1Proc?.WaitForExit(3000);

                        _logger.LogInformation("Executing taskkill for chromedriver.exe");
                        var tk2 = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = "/F /IM chromedriver.exe /T",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        var tk2Proc = System.Diagnostics.Process.Start(tk2);
                        tk2Proc?.WaitForExit(3000);
                        _logger.LogInformation("✓ taskkill executed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "taskkill failed");
                    }

                    await Task.Delay(500);

                    try
                    {
                        // Method 3: .NET Process.Kill() - Final cleanup
                        _logger.LogInformation("Executing .NET Process.Kill() cleanup");
                        var allChrome = System.Diagnostics.Process.GetProcessesByName("chrome");
                        var allDriver = System.Diagnostics.Process.GetProcessesByName("chromedriver");

                        foreach (var p in allChrome.Concat(allDriver))
                        {
                            try
                            {
                                if (!p.HasExited)
                                {
                                    p.Kill(true);
                                }
                            }
                            catch { }
                            finally
                            {
                                try { p.Dispose(); } catch { }
                            }
                        }
                        _logger.LogInformation("✓ .NET Process.Kill() cleanup completed");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ".NET kill failed");
                    }

                    // Verify
                    await Task.Delay(1000);
                    var remaining = System.Diagnostics.Process.GetProcessesByName("chrome").Length +
                                   System.Diagnostics.Process.GetProcessesByName("chromedriver").Length;

                    if (remaining == 0)
                    {
                        _logger.LogInformation("✅✅✅ ALL CHROME KILLED SUCCESSFULLY!");
                    }
                    else
                    {
                        _logger.LogError($"❌❌❌ {remaining} Chrome processes STILL ALIVE!");
                    }

                    // Chrome already killed above
                    _logger.LogInformation("Campaign stopped successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error closing browser session for campaign {id}");
                }

                _logger.LogInformation($"Campaign stopped: {campaign.Name} (ID: {campaign.Id})");

                return Ok(new {
                    message = "Campaign stopped successfully. Browser session closed and all pending messages cancelled.",
                    status = "Stopped"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error stopping campaign {id}");
                return StatusCode(500, new { error = "Internal server error while stopping campaign" });
            }
        }

        /// <summary>
        /// Emergency endpoint to forcefully close ALL Chrome browser sessions.
        /// Use this when stop campaign doesn't close Chrome properly.
        /// </summary>
        /// <returns>Result with number of processes killed</returns>
        /// <response code="200">Browsers closed successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Error closing browsers</response>
        [HttpPost("force-close-browsers")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> ForceCloseBrowsers()
        {
            try
            {
                // SECURITY FIX: Add API key validation
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                _logger.LogWarning($"FORCE CLOSE BROWSERS called by user {apiKeyEntity.UserId} - killing all Chrome processes");

                var browserSessionManager = HttpContext.RequestServices.GetRequiredService<IBrowserSessionManager>();
                browserSessionManager.CloseAllBrowsers();

                // Also kill any remaining Chrome/ChromeDriver processes directly
                int killedCount = 0;
                try
                {
                    var chromeProcesses = System.Diagnostics.Process.GetProcessesByName("chrome");
                    var chromedriverProcesses = System.Diagnostics.Process.GetProcessesByName("chromedriver");

                    _logger.LogInformation($"Found {chromeProcesses.Length} chrome and {chromedriverProcesses.Length} chromedriver processes to force kill");

                    foreach (var process in chromeProcesses.Concat(chromedriverProcesses))
                    {
                        try
                        {
                            // Check if already exited
                            if (process.HasExited)
                            {
                                _logger.LogDebug($"Process {process.Id} already exited");
                                process.Dispose();
                                continue;
                            }

                            _logger.LogInformation($"Force killing process: {process.ProcessName} (PID: {process.Id})");
                            process.Kill(entireProcessTree: true);

                            if (process.WaitForExit(2000))
                            {
                                killedCount++;
                                _logger.LogInformation($"✓ Killed {process.ProcessName} (PID: {process.Id})");
                            }
                            else
                            {
                                _logger.LogWarning($"Process {process.Id} did not exit within timeout");
                            }
                        }
                        catch (System.ComponentModel.Win32Exception win32Ex)
                        {
                            _logger.LogWarning($"Win32Exception: {win32Ex.Message} (Error: {win32Ex.NativeErrorCode})");
                        }
                        catch (InvalidOperationException)
                        {
                            _logger.LogDebug($"Process {process.Id} already exited");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Could not kill process {process.Id}");
                        }
                        finally
                        {
                            try { process.Dispose(); } catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during force kill of Chrome processes");
                }

                _logger.LogInformation($"✅ Force closed all browsers. Killed {killedCount} processes.");

                return Ok(new
                {
                    message = $"Successfully force closed all Chrome browsers. Killed {killedCount} processes.",
                    processesKilled = killedCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ForceCloseBrowsers");
                return StatusCode(500, new { error = "Error force closing browsers: " + ex.Message });
            }
        }

        /// <summary>
        /// Delete a campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Deletion result</returns>
        /// <remarks>
        /// **Note**: Running campaigns cannot be deleted. Stop the campaign first.
        /// </remarks>
        /// <response code="200">Campaign deleted successfully</response>
        /// <response code="400">Cannot delete running campaign</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                var campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Don't allow deleting running campaigns
                if (campaign.Status == CampaignStatus.Running)
                {
                    return BadRequest(new { error = "Cannot delete a running campaign. Stop it first." });
                }

                _context.Campaigns.Remove(campaign);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Campaign deleted: {campaign.Name} (ID: {campaign.Id})");

                return Ok(new { message = "Campaign deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting campaign {id}");
                return StatusCode(500, new { error = "Internal server error while deleting campaign" });
            }
        }

        /// <summary>
        /// Create a new campaign with workflow entries for specified contacts
        /// </summary>
        /// <param name="request">Campaign name and contact IDs</param>
        /// <returns>The created campaign with workflow count</returns>
        /// <remarks>
        /// Creates a campaign with status 'New' and generates CampaignWorkflow entries
        /// for each contact ID provided. Each workflow entry starts with 'Pending' status.
        ///
        /// Sample request:
        ///
        ///     POST /api/campaigns/with-workflow
        ///     {
        ///         "name": "My Marketing Campaign",
        ///         "contactIds": [1, 2, 3, 4, 5]
        ///     }
        ///
        /// </remarks>
        /// <response code="201">Campaign created successfully with workflow entries</response>
        /// <response code="400">Invalid request (no contacts, contacts not found)</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("with-workflow")]
        [ProducesResponseType(typeof(CreateCampaignWithWorkflowResponse), 201)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> CreateCampaignWithWorkflow([FromBody] CreateCampaignWithWorkflowRequest request)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Validate request
                if (request.ContactIds == null || request.ContactIds.Length == 0)
                {
                    return BadRequest(new { error = "At least one contact ID is required" });
                }

                // Validate that contacts exist and belong to the user
                var distinctContactIds = request.ContactIds.Distinct().ToList();
                var existingContacts = await _context.Contacts
                    .Where(c => distinctContactIds.Contains(c.Id) && c.UserId == userId)
                    .Select(c => c.Id)
                    .ToListAsync();

                if (existingContacts.Count == 0)
                {
                    return BadRequest(new { error = "No valid contacts found for the provided contact IDs" });
                }

                var invalidContactIds = distinctContactIds.Except(existingContacts).ToList();
                if (invalidContactIds.Any())
                {
                    _logger.LogWarning($"Some contact IDs were not found or don't belong to user: {string.Join(", ", invalidContactIds)}");
                }

                // Create campaign with transaction
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Create the campaign with 'New' status
                    var random = new Random();
                    var randomNumber = random.Next(100000, 999999); // 6-digit random number
                    // Create the campaign with 'New' status
                    var campaign = new Campaign
                    {
                        Name = $"Campaign-{randomNumber}",
                        Description = $"Campaign with {existingContacts.Count} contacts",
                        UserId = userId,
                        Status = CampaignStatus.New,
                        TotalContacts = existingContacts.Count,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.Campaigns.Add(campaign);
                    await _context.SaveChangesAsync();

                    // Get first attachment if any
                    string? attachmentBase64 = null;
                    string? attachmentFileName = null;
                    string? attachmentContentType = null;
                    long? attachmentSize = null;
                    string? attachmentType = null;

                    if (request.Attachments != null && request.Attachments.Count > 0)
                    {
                        var firstAttachment = request.Attachments[0];
                        attachmentBase64 = firstAttachment.Base64Data;
                        attachmentFileName = firstAttachment.FileName;
                        attachmentContentType = firstAttachment.ContentType;
                        attachmentSize = firstAttachment.Size;

                        // Determine attachment type from content type
                        if (!string.IsNullOrEmpty(firstAttachment.ContentType))
                        {
                            var contentType = firstAttachment.ContentType.ToLower();
                            if (contentType.StartsWith("image/"))
                                attachmentType = "Image";
                            else if (contentType.StartsWith("video/"))
                                attachmentType = "Video";
                            else if (contentType.StartsWith("audio/"))
                                attachmentType = "Audio";
                            else
                                attachmentType = "Document";
                        }
                    }

                    // Create CampaignWorkflow entries for each contact
                    var workflows = existingContacts.Select(contactId => new CampaignWorkflow
                    {
                        CampaignId = campaign.Id,
                        ContactId = contactId,
                        WorkflowStatus = WorkflowStatus.New,
                        AddedAt = DateTime.UtcNow,
                        MaleMessage = request.MaleMessage,
                        FemaleMessage = request.FemaleMessage,
                        AttachmentBase64 = attachmentBase64,
                        AttachmentFileName = attachmentFileName,
                        AttachmentContentType = attachmentContentType,
                        AttachmentSize = attachmentSize,
                        AttachmentType = attachmentType
                    }).ToList();

                    _context.CampaignWorkflows.AddRange(workflows);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation($"Campaign created with workflow: {campaign.Name} (ID: {campaign.Id}) with {workflows.Count} workflow entries for user {userId}");

                    var response = new CreateCampaignWithWorkflowResponse
                    {
                        Id = campaign.Id,
                        Name = campaign.Name,
                        Status = campaign.Status.ToString().ToLower(),
                        ContactsCount = workflows.Count,
                        CreatedAt = campaign.CreatedAt
                    };

                    return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, response);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating campaign with workflow");
                return StatusCode(500, new { error = "Internal server error while creating campaign with workflow" });
            }
        }

        /// <summary>
        /// Get workflow entries for a campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <param name="status">Optional filter by workflow status</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
        /// <returns>List of workflow entries</returns>
        /// <response code="200">Workflow entries returned successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}/workflows")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCampaignWorkflows(
            int id,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Verify campaign exists and belongs to user
                var campaign = await _context.Campaigns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Build query
                var query = _context.CampaignWorkflows
                    .AsNoTracking()
                    .Where(w => w.CampaignId == id);

                // Apply status filter if provided
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkflowStatus>(status, true, out var workflowStatus))
                {
                    query = query.Where(w => w.WorkflowStatus == workflowStatus);
                }

                var totalCount = await query.CountAsync();
                var workflows = await query
                    .OrderBy(w => w.AddedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(w => new CampaignWorkflowResponse
                    {
                        Id = w.Id,
                        CampaignId = w.CampaignId,
                        ContactId = w.ContactId,
                        WorkflowStatus = w.WorkflowStatus.ToString().ToLower(),
                        AddedAt = w.AddedAt,
                        ProcessedAt = w.ProcessedAt,
                        ErrorMessage = w.ErrorMessage,
                        RetryCount = w.RetryCount,
                        MaleMessage = w.MaleMessage,
                        FemaleMessage = w.FemaleMessage,
                        HasAttachment = w.AttachmentBase64 != null,
                        AttachmentFileName = w.AttachmentFileName,
                        AttachmentContentType = w.AttachmentContentType,
                        AttachmentSize = w.AttachmentSize,
                        AttachmentType = w.AttachmentType
                    })
                    .ToListAsync();

                return Ok(new
                {
                    workflows,
                    totalCount,
                    page,
                    pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving workflows for campaign {id}");
                return StatusCode(500, new { error = "Internal server error while retrieving workflows" });
            }
        }

        /// <summary>
        /// Get workflow summary/statistics for a campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Workflow statistics by status</returns>
        /// <response code="200">Summary returned successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}/workflow-summary")]
        [ProducesResponseType(typeof(CampaignWorkflowSummary), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCampaignWorkflowSummary(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Get campaign
                var campaign = await _context.Campaigns
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Get workflow statistics grouped by status
                var statusCounts = await _context.CampaignWorkflows
                    .Where(w => w.CampaignId == id)
                    .GroupBy(w => w.WorkflowStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                var summary = new CampaignWorkflowSummary
                {
                    CampaignId = campaign.Id,
                    CampaignName = campaign.Name,
                    CampaignStatus = campaign.Status.ToString().ToLower(),
                    TotalWorkflows = statusCounts.Sum(s => s.Count),
                    PendingCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Pending)?.Count ?? 0,
                    ProcessingCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Processing)?.Count ?? 0,
                    SentCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Sent)?.Count ?? 0,
                    DeliveredCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Delivered)?.Count ?? 0,
                    FailedCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Failed)?.Count ?? 0,
                    BouncedCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Bounced)?.Count ?? 0,
                    OpenedCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Opened)?.Count ?? 0,
                    ClickedCount = statusCounts.FirstOrDefault(s => s.Status == WorkflowStatus.Clicked)?.Count ?? 0,
                    CreatedAt = campaign.CreatedAt
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving workflow summary for campaign {id}");
                return StatusCode(500, new { error = "Internal server error while retrieving workflow summary" });
            }
        }

        /// <summary>
        /// Get detailed progress information for a campaign
        /// </summary>
        /// <param name="id">Campaign ID</param>
        /// <returns>Detailed progress including statistics and timing</returns>
        /// <remarks>
        /// Returns comprehensive progress data including:
        /// - Progress percentage and contact counts
        /// - Statistics (sent, delivered, failed, success rate)
        /// - Timing information (started at, estimated completion)
        /// - Error information
        ///
        /// Sample response:
        ///
        ///     {
        ///         "campaign_id": 1,
        ///         "campaign_name": "My Campaign",
        ///         "status": "Running",
        ///         "progress": {
        ///             "total_contacts": 100,
        ///             "processed": 50,
        ///             "pending": 50,
        ///             "percentage": 50.00
        ///         },
        ///         "statistics": {
        ///             "sent": 48,
        ///             "delivered": 45,
        ///             "failed": 2,
        ///             "success_rate": 96.00
        ///         }
        ///     }
        ///
        /// </remarks>
        /// <response code="200">Progress data returned successfully</response>
        /// <response code="401">Invalid or missing API key</response>
        /// <response code="404">Campaign not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id}/progress")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCampaignProgress(int id)
        {
            try
            {
                // Validate API key
                var apiKey = Request.Headers["X-API-Key"].FirstOrDefault();
                if (string.IsNullOrEmpty(apiKey))
                {
                    return Unauthorized(new { error = "API key is required" });
                }

                var apiKeyEntity = await _apiKeyService.ValidateApiKeyAsync(apiKey);
                if (apiKeyEntity == null)
                {
                    return Unauthorized(new { error = "Invalid API key" });
                }

                var userId = apiKeyEntity.UserId;

                // Get campaign with minimal data for performance
                var campaign = await _context.Campaigns
                    .AsNoTracking()
                    .Where(c => c.Id == id && c.UserId == userId)
                    .Select(c => new
                    {
                        c.Id,
                        c.Name,
                        c.Status,
                        c.TotalContacts,
                        c.MessagesSent,
                        c.MessagesDelivered,
                        c.MessagesFailed,
                        c.CurrentProgress,
                        c.StartedAt,
                        c.UpdatedAt,
                        c.LastError,
                        c.ErrorCount
                    })
                    .FirstOrDefaultAsync();

                if (campaign == null)
                {
                    return NotFound(new { error = "Campaign not found" });
                }

                // Get contact status breakdown for detailed progress
                var contactStats = await _context.Contacts
                    .Where(c => c.CampaignId == id)
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync();

                // Calculate metrics
                var totalContacts = campaign.TotalContacts;
                var processed = campaign.CurrentProgress;
                var pending = contactStats.FirstOrDefault(s => s.Status == ContactStatus.Pending)?.Count ?? 0;
                var sent = contactStats.FirstOrDefault(s => s.Status == ContactStatus.Sent)?.Count ?? 0;
                var delivered = contactStats.FirstOrDefault(s => s.Status == ContactStatus.Delivered)?.Count ?? 0;
                var failed = contactStats.FirstOrDefault(s => s.Status == ContactStatus.Failed)?.Count ?? 0;

                var progressPercentage = totalContacts > 0
                    ? (decimal)processed / totalContacts * 100
                    : 0;

                var successRate = processed > 0
                    ? (decimal)sent / processed * 100
                    : 0;

                // Calculate estimated time remaining
                TimeSpan? estimatedTimeRemaining = null;
                if (campaign.Status == CampaignStatus.Running && campaign.StartedAt.HasValue && processed > 0)
                {
                    var elapsed = DateTime.UtcNow - campaign.StartedAt.Value;
                    var avgTimePerContact = elapsed.TotalSeconds / processed;
                    var remaining = totalContacts - processed;
                    estimatedTimeRemaining = TimeSpan.FromSeconds(avgTimePerContact * remaining);
                }

                var response = new
                {
                    campaign_id = campaign.Id,
                    campaign_name = campaign.Name,
                    status = campaign.Status.ToString(),
                    progress = new
                    {
                        total_contacts = totalContacts,
                        processed = processed,
                        pending = pending,
                        percentage = Math.Round(progressPercentage, 2)
                    },
                    statistics = new
                    {
                        sent = sent,
                        delivered = delivered,
                        failed = failed,
                        success_rate = Math.Round(successRate, 2)
                    },
                    timing = new
                    {
                        started_at = campaign.StartedAt,
                        last_updated = campaign.UpdatedAt,
                        estimated_completion = estimatedTimeRemaining.HasValue
                            ? DateTime.UtcNow.Add(estimatedTimeRemaining.Value)
                            : (DateTime?)null,
                        estimated_time_remaining_seconds = estimatedTimeRemaining?.TotalSeconds
                    },
                    errors = new
                    {
                        count = campaign.ErrorCount,
                        last_error = campaign.LastError
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving campaign progress for ID {id}");
                return StatusCode(500, new { error = "Internal server error while retrieving progress" });
            }
        }
    }
}
