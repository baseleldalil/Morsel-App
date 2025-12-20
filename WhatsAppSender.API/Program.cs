using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WhatsApp.Shared.Data;
using WhatsApp.Shared.Services;
using WhatsAppSender.API;
using WhatsAppSender.API.Models;
using WhatsAppSender.API.Services;
using Npgsql;

// Fix for Npgsql TimeSpan overflow with PostgreSQL timestamps
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add Memory Cache for performance optimization
builder.Services.AddMemoryCache();

// Add HttpClient factory
builder.Services.AddHttpClient();

// Configure settings from appsettings.json
builder.Services.Configure<AutoModeSendingSettings>(
    builder.Configuration.GetSection(AutoModeSendingSettings.SectionName));
builder.Services.Configure<WhatsAppSettings>(
    builder.Configuration.GetSection(WhatsAppSettings.SectionName));
builder.Services.Configure<ApiKeySettings>(
    builder.Configuration.GetSection(ApiKeySettings.SectionName));
builder.Services.Configure<TranslationSettings>(
    builder.Configuration.GetSection(TranslationSettings.SectionName));
builder.Services.Configure<ContactsSettings>(
    builder.Configuration.GetSection(ContactsSettings.SectionName));

// Database context (PostgreSQL) - Use NpgsqlDataSource to avoid timeout calculation issues
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<SaaSDbContext>(options =>
{
    options.UseNpgsql(dataSource, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(30);
    });
});

// Register shared SaaS services
builder.Services.AddScoped<ISaaSApiKeyService, SaaSApiKeyService>();
builder.Services.AddScoped<ISaaSSubscriptionService, SaaSSubscriptionService>();

// Register WhatsApp API services
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<IContactProcessingService, ContactProcessingService>();
builder.Services.AddScoped<IContactAttachmentService, ContactAttachmentService>(); // Contact attachment service
builder.Services.AddScoped<IPhoneExtractionService, PhoneExtractionService>(); // Phone extraction for multi-number support (Req #3)
builder.Services.AddScoped<IDuplicatePreventionService, DuplicatePreventionService>(); // Duplicate prevention service (Req #4)
builder.Services.AddScoped<IMessageTemplateService, MessageTemplateService>();
builder.Services.AddScoped<ITemplateValidationService, TemplateValidationService>(); // Template validation service
builder.Services.AddScoped<ITimingService, TimingService>();
builder.Services.AddScoped<IAdvancedTimingService, AdvancedTimingService>(); // Advanced timing with decimal delays
builder.Services.AddScoped<IDeliveryTrackingService, DeliveryTrackingService>();
builder.Services.AddScoped<IMessageSendingService, MessageSendingService>();
builder.Services.AddSingleton<IBrowserSessionManager, BrowserSessionManager>();
builder.Services.AddSingleton<ICampaignExecutorService, CampaignExecutorService>();
builder.Services.AddScoped<IWorkflowCampaignService, WorkflowCampaignService>(); // Workflow campaign execution service

// Translation services
builder.Services.AddHttpClient<LibreTranslateService>();
builder.Services.AddScoped<LibreTranslateService>();
builder.Services.AddScoped<TranslationServiceFactory>();
builder.Services.AddScoped<ITranslationService>(provider =>
{
    var factory = provider.GetRequiredService<TranslationServiceFactory>();
    return factory.CreateTranslationService();
});



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WhatsApp Sender API",
        Version = "v1",
        Description = @"
## WhatsApp Sender API Documentation

API for WhatsApp contact management, messaging, and campaign automation.

### Authentication
All endpoints require an API key passed in the `X-API-Key` header.

### Campaign Workflow
1. **Create Campaign** - `POST /api/campaigns`
2. **Start Campaign** - `POST /api/campaigns/{id}/start`
3. **Monitor Progress** - `GET /api/campaigns/{id}/progress`
4. **Pause Campaign** - `POST /api/campaigns/{id}/pause`
5. **Resume Campaign** - `POST /api/campaigns/{id}/start` (on paused campaign)
6. **Stop Campaign** - `POST /api/campaigns/{id}/stop`

### Campaign Status Flow
```
Pending -> Running -> Paused -> Running -> Completed
                  \-> Stopped (cannot restart)
```
",
        Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@whatsapp-sender.io"
        }
    });

    // ✅ API Key authentication (header) - Updated to use X-API-Key
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key authentication. Enter your API key in the `X-API-Key` header.",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-API-Key",  // The header name your API expects
        In = ParameterLocation.Header,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });

    // ✅ Enable file uploads for Excel/CSV
    c.OperationFilter<FileUploadOperationFilter>();

    // Avoid schema conflicts
    c.CustomSchemaIds(type => type.FullName);

    // ✅ Include XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // ✅ Tag descriptions for grouping endpoints
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});

// ✅ Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ✅ Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();


// ✅ Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable Swagger always (dev + prod)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WhatsApp Sender API V1");
    c.RoutePrefix = string.Empty; // Access directly from root (http://localhost:5000)
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
});

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();


// ✅ Initialize Shared Database
using (var scope = app.Services.CreateScope())
{
    var saasContext = scope.ServiceProvider.GetRequiredService<SaaSDbContext>();

    Console.WriteLine("📊 Initializing WhatsApp SaaS shared database...");

    // ✅ Create missing tables if they don't exist
    try
    {
        // Check and create SentPhoneNumbers table
        var createSentPhoneNumbersTable = @"
            CREATE TABLE IF NOT EXISTS ""SentPhoneNumbers"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""UserId"" VARCHAR(450) NOT NULL,
                ""PhoneNumber"" VARCHAR(100) NOT NULL,
                ""FirstSentAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""LastSentAt"" TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT (NOW() AT TIME ZONE 'utc'),
                ""SendCount"" INTEGER NOT NULL DEFAULT 1,
                ""LastCampaignId"" INTEGER NULL,
                ""LastStatus"" VARCHAR(50) NULL
            );

            CREATE INDEX IF NOT EXISTS ""IX_SentPhoneNumbers_PhoneNumber""
                ON ""SentPhoneNumbers"" (""PhoneNumber"");

            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_SentPhoneNumbers_UserId_PhoneNumber""
                ON ""SentPhoneNumbers"" (""UserId"", ""PhoneNumber"");
        ";

        await saasContext.Database.ExecuteSqlRawAsync(createSentPhoneNumbersTable);
        Console.WriteLine("✅ SentPhoneNumbers table verified/created");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️ Warning creating SentPhoneNumbers table: {ex.Message}");
    }

    Console.WriteLine("✅ Shared database initialized successfully");

    var subscriptionCount = await saasContext.SubscriptionPlans.CountAsync();
    Console.WriteLine($"📦 Found {subscriptionCount} subscription plans in shared database");

    var templateCount = await saasContext.CampaignTemplates.CountAsync();
    Console.WriteLine($"📝 Found {templateCount} campaign templates in shared database");
}

app.Run();
