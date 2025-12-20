using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WhatsApp.Shared.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CompanyName = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ArabicName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EnglishName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FormattedPhone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IssueDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastMessageSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastStatusUpdateAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SendAttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApiKeyId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxMessagesPerDay = table.Column<int>(type: "integer", nullable: false),
                    MaxApiKeys = table.Column<int>(type: "integer", nullable: false),
                    MaxCampaignTemplates = table.Column<int>(type: "integer", nullable: false),
                    MaxContactsPerCampaign = table.Column<int>(type: "integer", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasCustomIntegrations = table.Column<bool>(type: "boolean", nullable: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    HasScheduledMessages = table.Column<bool>(type: "boolean", nullable: false),
                    HasTemplateVariables = table.Column<bool>(type: "boolean", nullable: false),
                    HasMultiLanguageSupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasWhatsAppBotIntegration = table.Column<bool>(type: "boolean", nullable: false),
                    HasCustomBranding = table.Column<bool>(type: "boolean", nullable: false),
                    TrialDurationDays = table.Column<int>(type: "integer", nullable: false),
                    BillingCycleDays = table.Column<int>(type: "integer", nullable: false),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "numeric", nullable: true),
                    DiscountValidUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MessageTemplate = table.Column<string>(type: "text", nullable: false),
                    MaleContent = table.Column<string>(type: "text", nullable: true),
                    FemaleContent = table.Column<string>(type: "text", nullable: true),
                    AutoGenerateGenderVersions = table.Column<bool>(type: "boolean", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    HasAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentPath = table.Column<string>(type: "text", nullable: true),
                    AttachmentName = table.Column<string>(type: "text", nullable: true),
                    AttachmentType = table.Column<string>(type: "text", nullable: true),
                    SupportsVariables = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableVariables = table.Column<string>(type: "text", nullable: true),
                    IsScheduled = table.Column<bool>(type: "boolean", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScheduleRecurrence = table.Column<string>(type: "text", nullable: true),
                    LastScheduledRun = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextScheduledRun = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SupportsMultiLanguage = table.Column<bool>(type: "boolean", nullable: false),
                    DefaultLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TranslatedTemplates = table.Column<string>(type: "text", nullable: true),
                    TimesUsed = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuccessRate = table.Column<decimal>(type: "numeric", nullable: false),
                    TargetAudience = table.Column<string>(type: "text", nullable: true),
                    MaxRecipientsPerBatch = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignTemplates_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsageStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MessagesSent = table.Column<int>(type: "integer", nullable: false),
                    MessagesDelivered = table.Column<int>(type: "integer", nullable: false),
                    MessagesFailed = table.Column<int>(type: "integer", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
                    ApiCallsMade = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsageStatistics_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: false),
                    DailyQuotaUsed = table.Column<int>(type: "integer", nullable: false),
                    TotalMessagesUsed = table.Column<int>(type: "integer", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsageResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiKeys_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessageTimingControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxDelaySeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTimingControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageTimingControls_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RandomDelayRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AfterMessageCount = table.Column<int>(type: "integer", nullable: false),
                    PauseDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    RandomVarianceSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomDelayRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RandomDelayRules_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    IsTrialPeriod = table.Column<bool>(type: "boolean", nullable: false),
                    IsInGracePeriod = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TrialEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodEndsAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CurrentPeriodMessages = table.Column<int>(type: "integer", nullable: false),
                    LastUsageResetAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSubscriptions_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VideoTimingControls",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinDelayBeforeUploadSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxDelayBeforeUploadSeconds = table.Column<int>(type: "integer", nullable: false),
                    MinUploadTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxUploadTimeSeconds = table.Column<int>(type: "integer", nullable: false),
                    MinDelayAfterUploadSeconds = table.Column<int>(type: "integer", nullable: false),
                    MaxDelayAfterUploadSeconds = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubscriptionPlanId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideoTimingControls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideoTimingControls_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MessageContent = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    HasAttachment = table.Column<bool>(type: "boolean", nullable: false),
                    AttachmentName = table.Column<string>(type: "text", nullable: true),
                    AttachmentType = table.Column<string>(type: "text", nullable: true),
                    AttachmentSize = table.Column<long>(type: "bigint", nullable: true),
                    ApiKeyUsed = table.Column<string>(type: "text", nullable: true),
                    CampaignId = table.Column<int>(type: "integer", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MessageHistory_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageHistory_CampaignTemplates_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TemplateAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CampaignTemplateId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TemplateAttachments_CampaignTemplates_CampaignTemplateId",
                        column: x => x.CampaignTemplateId,
                        principalTable: "CampaignTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SubscriptionId = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PaymentReference = table.Column<string>(type: "text", nullable: true),
                    PaymentGatewayResponse = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_UserSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "UserSubscriptions",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "SubscriptionPlans",
                columns: new[] { "Id", "BillingCycleDays", "CreatedAt", "Description", "DiscountPercentage", "DiscountValidUntil", "DisplayOrder", "GracePeriodDays", "HasAdvancedAnalytics", "HasCustomBranding", "HasCustomIntegrations", "HasMultiLanguageSupport", "HasPrioritySupport", "HasScheduledMessages", "HasTemplateVariables", "HasWhatsAppBotIntegration", "IsActive", "MaxApiKeys", "MaxCampaignTemplates", "MaxContactsPerCampaign", "MaxMessagesPerDay", "Name", "Price", "TrialDurationDays", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Try our service free for 14 days - no credit card required", null, null, 1, 3, false, false, false, false, false, false, false, false, true, 1, 3, 25, 50, "Free Trial", 0m, 14, null },
                    { 2, 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Perfect for small businesses getting started with WhatsApp marketing", null, null, 2, 3, false, false, false, false, false, true, true, false, true, 2, 10, 100, 100, "Starter", 9.99m, 7, null },
                    { 3, 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Ideal for growing businesses with moderate messaging needs", null, null, 3, 5, true, false, false, true, true, true, true, false, true, 5, 25, 500, 500, "Professional", 29.99m, 7, null },
                    { 4, 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "For large businesses with high-volume messaging requirements", null, null, 4, 7, true, true, true, true, true, true, true, true, true, 10, 100, 2000, 2000, "Business", 99.99m, 14, null },
                    { 5, 30, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Unlimited messaging for enterprise-level operations with dedicated support", null, null, 5, 14, true, true, true, true, true, true, true, true, true, 25, -1, 10000, 10000, "Enterprise", 299.99m, 14, null }
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "Category", "Description", "Key", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1, "General", "The name of the SaaS platform", "SAAS_NAME", new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3243), null, "WhatsApp SaaS Platform" },
                    { 2, "WhatsApp", "Base URL for WhatsApp API service", "WHATSAPP_API_URL", new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3599), null, "https://localhost:7001/api" },
                    { 3, "Billing", "Cost per message in USD", "MESSAGE_COST_PER_UNIT", new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3601), null, "0.05" },
                    { 4, "General", "Default timezone for the application", "DEFAULT_TIMEZONE", new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3603), null, "UTC" },
                    { 5, "WhatsApp", "Maximum file upload size in bytes (10MB)", "MAX_FILE_UPLOAD_SIZE", new DateTime(2025, 10, 29, 17, 44, 38, 937, DateTimeKind.Utc).AddTicks(3604), null, "10485760" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_SubscriptionPlanId",
                table: "ApiKeys",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_UserId_IsActive",
                table: "ApiKeys",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignTemplates_UserId",
                table: "CampaignTemplates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_ApiKeyId",
                table: "Contacts",
                column: "ApiKeyId");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_FormattedPhone",
                table: "Contacts",
                column: "FormattedPhone");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_Status",
                table: "Contacts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_UserId",
                table: "Contacts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistory_CampaignId",
                table: "MessageHistory",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageHistory_UserId_SentAt",
                table: "MessageHistory",
                columns: new[] { "UserId", "SentAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageTimingControls_IsActive",
                table: "MessageTimingControls",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTimingControls_SubscriptionPlanId",
                table: "MessageTimingControls",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_SubscriptionId",
                table: "PaymentTransactions",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_UserId",
                table: "PaymentTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RandomDelayRules_IsActive",
                table: "RandomDelayRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RandomDelayRules_Priority",
                table: "RandomDelayRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RandomDelayRules_SubscriptionPlanId",
                table: "RandomDelayRules",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateAttachments_CampaignTemplateId",
                table: "TemplateAttachments",
                column: "CampaignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_UserId_Date",
                table: "UsageStatistics",
                columns: new[] { "UserId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_SubscriptionPlanId",
                table: "UserSubscriptions",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_UserId",
                table: "UserSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTimingControls_IsActive",
                table: "VideoTimingControls",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_VideoTimingControls_SubscriptionPlanId",
                table: "VideoTimingControls",
                column: "SubscriptionPlanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "MessageHistory");

            migrationBuilder.DropTable(
                name: "MessageTimingControls");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "RandomDelayRules");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TemplateAttachments");

            migrationBuilder.DropTable(
                name: "UsageStatistics");

            migrationBuilder.DropTable(
                name: "VideoTimingControls");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "CampaignTemplates");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.DropTable(
                name: "AspNetUsers");
        }
    }
}
