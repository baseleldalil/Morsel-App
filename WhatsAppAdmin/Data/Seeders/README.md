# 🌱 Morsel WhatsApp SaaS - Data Seeders

This document explains the comprehensive data seeding system for the Morsel WhatsApp SaaS platform.

## 📁 Seeder Structure

```
Data/Seeders/
├── MasterSeeder.cs          # Main coordinator for all seeders
├── UserSeeder.cs            # Admin users and roles
├── UserSubscriptionSeeder.cs # User subscription assignments
├── RunSeeders.cs            # Manual execution utilities
└── README.md               # This documentation
```

## 🚀 Automatic Seeding

Seeders run automatically when applications start:

**WhatsAppAdmin Project:**
- **Development Mode**: Full admin seeding (users, roles, subscriptions)
- **Production Mode**: Minimal seeding with essential data only

**WhatsAppSender.API Project:**
- **Development Mode**: Full API key seeding with realistic data
- **Production Mode**: No automatic API key seeding

## 👥 Admin Users Created

The `UserSeeder` creates the following admin users:

| Email | Role | Password | Company |
|-------|------|----------|---------|
| `superadmin@morsel.com` | SuperAdmin | `SuperAdmin123!` | Morsel Technologies |
| `admin@morsel.com` | Admin | `Admin123!` | Morsel Technologies |
| `semiadmin@morsel.com` | SemiAdmin | `SemiAdmin123!` | Morsel Technologies |
| `manager@morsel.com` | Manager | `Manager123!` | Morsel Technologies |
| `support@morsel.com` | Support | `Support123!` | Morsel Technologies |
| `regional@morsel.com` | Manager, Support | `Regional123!` | Morsel UAE |
| `demo@morsel.com` | SemiAdmin | `Demo123!` | Demo Company |

## 📋 User Subscription Assignments

The system creates realistic user assignments with different patterns:

### Business Users (Professional/Enterprise)
- **ahmed.ali@techcorp.ae** - Professional Plan (45 messages used today)
- **sara.mohammed@digitalsolutions.com** - Enterprise Plan (150 messages used)
- **fatima.ahmad@smartsystems.com** - Professional Plan (75 messages used)
- **khalid.ibrahim@futuretech.ae** - Enterprise Plan (300 messages used)
- **marketing@megacorp.ae** - Enterprise Plan (1800 messages - near limit)

### Basic Plan Users
- **omar.hassan@modernbusiness.ae** - Basic Plan (25 messages used)
- **new.customer@freshstart.com** - Basic Plan (fresh account, 0 messages)

### Trial Users
- **trial.user1@startup.com** - Free Trial (3 messages used, expires in 25 days)
- **trial.user2@newbusiness.ae** - Free Trial (1 message used, expires in 28 days)

### Inactive Users
- **expired.user@oldcompany.com** - Basic Plan (expired 30 days ago)

## 🔑 WhatsApp API Keys

The `ApiKeySeeder` creates realistic API keys linked to users:

### Key Features:
- **Realistic Usage Patterns**: Keys show different usage levels
- **Multiple Keys per User**: Enterprise users have multiple keys for different purposes
- **Usage Tracking**: Daily and total usage counters
- **Expiration Dates**: Keys with realistic expiration periods
- **Active/Inactive States**: Some keys are expired or revoked

### Sample API Keys:
- Production APIs for business users
- Development/Testing keys
- Marketing automation keys
- Customer support bot keys
- Legacy/expired keys for testing

## 🛠️ Manual Seeding

To run seeders manually, use the `RunSeeders` class:

```csharp
// In a controller or service
await RunSeeders.ExecuteAsync(adminContext, userManager, roleManager);
```

## 🔧 Utility Functions

### Clear All Data
```csharp
await RunSeeders.ClearAllDataAsync(context);
```

### Reset Specific Data
```csharp
await RunSeeders.ResetUserAssignmentsAsync(context);
```

## 📊 Seeding Output

When seeders run, you'll see console output like:

```
🌱 Starting comprehensive data seeding...

👥 Seeding Admin Users and Roles...
✅ Created user: superadmin@morsel.com with roles: SuperAdmin
✅ Created user: admin@morsel.com with roles: Admin
...

📋 Seeding User Subscription Assignments...
✅ Seeded 10 user subscription assignments
📊 User Assignment Summary:
   - Professional Plan: 3 users
   - Enterprise Plan: 3 users
   - Basic Plan: 2 users
   - Free Trial: 2 users

🔑 Seeding WhatsApp API Keys...
✅ Seeded 14 API keys
📊 API Key Summary:
   - Professional Plan: 4 keys (3 active), 2,125 total messages
   - Enterprise Plan: 6 keys (5 active), 26,950 total messages
   - Basic Plan: 3 keys (2 active), 2,950 total messages
   - Free Trial: 2 keys (2 active), 18 total messages

✅ Data seeding completed successfully!
```

## 🎯 Use Cases

### Development
- **Full Dataset**: Comprehensive test data for all features
- **Multiple User Types**: Test different subscription levels
- **Realistic Usage**: Various usage patterns and states

### Testing
- **API Testing**: Use seeded API keys for endpoint testing
- **User Scenarios**: Test different user permission levels
- **Edge Cases**: Expired accounts, heavy usage, trial users

### Demo/Presentation
- **Clean Data**: Professional-looking demo data
- **Arabic Support**: UAE-focused sample data
- **Multiple Scenarios**: Show various business cases

## ⚠️ Security Notes

- **Development Only**: Full seeding only runs in development mode
- **Strong Passwords**: All demo passwords follow security requirements
- **Realistic Data**: Uses professional UAE business context
- **No Real Data**: All seeded data is fictional

## 🔄 Database Refresh

To completely refresh seeded data:

**WhatsAppAdmin:**
1. Delete `Admin.db` file (SQLite)
2. Restart WhatsAppAdmin application
3. Admin seeders will automatically recreate everything

**WhatsAppSender.API:**
1. Delete `WhatsAppSender.db` file (SQLite)
2. Restart WhatsAppSender.API application
3. API key seeders will automatically recreate everything

## 🤝 Contributing

When adding new seeders:

1. Follow the existing naming conventions
2. Include comprehensive console output
3. Handle existing data gracefully
4. Update this documentation
5. Test both development and production modes

## 📝 Notes

- Seeders are idempotent (safe to run multiple times)
- Existing data is preserved when possible
- All timestamps use realistic dates
- Usage patterns reflect real-world scenarios
- Email addresses use appropriate domains (.com, .ae)