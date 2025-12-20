using Microsoft.EntityFrameworkCore;
using WhatsAppWebAutomation.Data;
using WhatsAppWebAutomation.Services;

// Enable legacy timestamp behavior for Npgsql to handle DateTime without strict Kind checking
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Make JSON property names case-insensitive (accept both camelCase and PascalCase)
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        // Use camelCase for output
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure CORS - Allow all origins for this automation tool
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure PostgreSQL DbContext
builder.Services.AddDbContext<WhatsAppSaasContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Register services as Singleton (keep browser session alive across requests)
builder.Services.AddSingleton<IBrowserService, BrowserService>();
builder.Services.AddSingleton<IBulkOperationManager, BulkOperationManager>();
builder.Services.AddSingleton<IWhatsAppService, WhatsAppService>();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
// Enable Swagger in all environments for this automation tool
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "WhatsApp Web Automation API v1");
    options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    options.DocumentTitle = "WhatsApp Web Automation API";
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "WhatsAppWebAutomation" }));

// Log startup information
var logger = app.Services.GetRequiredService<ILogger<Program>>();
var browserType = app.Configuration["BrowserSettings:BrowserType"] ?? "Chrome";
logger.LogInformation("WhatsApp Web Automation API starting...");
logger.LogInformation("Browser Type: {BrowserType}", browserType);
logger.LogInformation("Swagger UI available at: https://localhost:<port>/");

app.Run();
