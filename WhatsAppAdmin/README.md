# WhatsApp Admin Module

A standalone ASP.NET Core MVC admin panel for managing WhatsApp SaaS subscriptions, permissions, campaign templates, and user assignments.

## Features

### 🎛️ Subscription Management
- Create, edit, and delete subscription plans
- Define message quotas per day
- Set pricing and permissions for each plan
- Configure timer settings (min/max delay between messages)
- Track active/inactive subscriptions

### 👥 User Assignment Management
- Assign users to subscription plans
- Track message usage and quotas
- Monitor subscription expiry dates
- Reset daily message counts
- View usage statistics and analytics

### 📧 Campaign Template Management
- Create reusable message templates
- Organize templates by categories
- Include dynamic variables (name, company, etc.)
- Support for image attachments
- Enable/disable templates

### 🔐 Permissions System
- Granular permission control
- Assign permissions to subscription plans
- Predefined permissions:
  - CanCreateCampaign
  - CanUseAPI
  - CanAccessTemplates
  - CanSendBulkMessages
  - CanViewAnalytics
  - CanExportData
  - CanScheduleMessages
  - CanUseCustomFields

## Architecture

The application follows clean architecture principles:

```
Controllers → Services → Repositories → EF Core Models
```

### Models
- **Subscription**: Defines subscription plans with quotas and pricing
- **Permission**: Individual permissions that can be assigned to subscriptions
- **TimerSettings**: Message sending delay configurations
- **CampaignTemplate**: Reusable message templates
- **UserSubscription**: Links users to their assigned subscriptions
- **AdminUser**: Authentication model for admin access

### Services
- **ISubscriptionService**: Business logic for subscription management
- **ICampaignTemplateService**: Template management operations
- **IPermissionService**: Permission management
- **IUserSubscriptionService**: User assignment and usage tracking

### Repositories
- **ISubscriptionRepository**: Data access for subscriptions
- **ICampaignTemplateRepository**: Template data operations
- **IPermissionRepository**: Permission data access
- **IUserSubscriptionRepository**: User subscription data management

## Database Schema

### Key Relationships
- Subscription ↔ TimerSettings (1:1)
- Subscription ↔ Permission (Many-to-Many via SubscriptionPermission)
- Subscription ↔ UserSubscription (1:Many)
- AdminUser (Identity tables)

## Setup Instructions

### 1. Prerequisites
- .NET 9.0 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code

### 2. Database Configuration
Update `appsettings.json` with your database connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=WhatsAppAdminDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 3. Database Migration
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 4. Run the Application
```bash
dotnet run
```

### 5. First Time Setup
1. Navigate to `/Identity/Account/Register` to create the first admin user
2. The application will automatically seed sample data on first run:
   - Default permissions
   - Sample subscription plans (Free Trial, Basic, Professional, Enterprise)
   - Sample campaign templates

## Sample Data

### Default Subscription Plans

| Plan | Price | Messages/Day | Features |
|------|-------|--------------|----------|
| Free Trial | $0.00 | 10 | Basic API access |
| Basic Plan | $9.99 | 100 | API + Templates |
| Professional | $29.99 | 500 | Campaigns + Bulk + Scheduling |
| Enterprise | $99.99 | 2000 | All features |

### Sample Templates
- Welcome Message
- Order Confirmation
- Promotional Offer
- Appointment Reminder
- Support Follow-up

## Usage

### Managing Subscriptions
1. Go to **Subscriptions** → **Create New Subscription**
2. Define name, description, price, and daily message limit
3. Assign permissions that define what features users can access
4. Configure timer settings for message delays (optional)

### Assigning Users
1. Go to **User Assignments** → **Assign User**
2. Enter the user ID from your main WhatsApp application
3. Select a subscription plan
4. Set expiry date (optional)

### Creating Templates
1. Go to **Templates** → **Create New Template**
2. Write your message content with variables like `{name}`, `{company}`
3. Categorize the template
4. Add image URL if needed

### Monitoring Usage
- Dashboard shows overview statistics
- User Assignments page shows daily usage vs. quotas
- View expiring subscriptions
- Reset daily message counts if needed

## Integration with Main Application

### User ID Mapping
The admin module stores user assignments by `UserId` which should match the user ID from your main WhatsApp application.

### API Integration Points
Your main WhatsApp API should check:
1. **User subscription status**: `IUserSubscriptionService.CanUserSendMessageAsync(userId)`
2. **Increment usage**: `IUserSubscriptionService.IncrementUserMessageCountAsync(userId)`
3. **Daily resets**: `IUserSubscriptionService.ResetDailyMessageCountsAsync()` (via scheduled job)

### Timer Settings
Retrieve timer settings from user's subscription to implement message delays:
```csharp
var userSub = await _userSubscriptionService.GetUserSubscriptionByUserIdAsync(userId);
var timerSettings = userSub?.Subscription?.TimerSettings;
```

## Security

- Admin authentication required for all pages
- Identity framework with cookie authentication
- CSRF protection on all forms
- Input validation and sanitization

## Deployment

### Separate Deployment
This admin module is designed to be deployed separately from your main WhatsApp application:

1. Deploy to a separate server/container
2. Use shared database or API calls for integration
3. Secure admin access with appropriate network policies
4. Consider using a subdomain like `admin.yourwhatsappsaas.com`

### Environment Configuration
- Use environment-specific `appsettings.{Environment}.json`
- Store sensitive data in environment variables or Azure Key Vault
- Configure logging appropriately for production

## Contributing

1. Follow the existing architecture patterns
2. Add async/await for all database operations
3. Include comprehensive comments for new features
4. Update this README for new functionality
5. Test all features before submitting changes

## License

This admin module is part of the WhatsApp SaaS system and follows the same licensing terms as the main application.