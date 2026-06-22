# Super Admin Seeding Documentation

## Overview
The application now includes automatic seeding of a super admin account in the Infrastructure layer. This super admin account is created when the database is initialized.

## Super Admin Credentials

### Email
```
admin@tibr.com
```

### Password
```
SuperAdmin@123
```

## Account Details

| Field | Value |
|-------|-------|
| **First Name** | Super |
| **Last Name** | Admin |
| **Email** | admin@tibr.com |
| **Phone** | +1-000-000-0000 |
| **Status** | Active |
| **OTP Verified** | Yes |
| **KYC Status** | Verified |
| **Role** | Admin |

## How It Works

### 1. **SeedData Class** (`Tibr.Infrastructure/Seeds/SeedData.cs`)
   - Contains both synchronous and asynchronous seed methods
   - `SeedSuperAdmin()` - Synchronous method for immediate seeding
   - `SeedSuperAdminAsync()` - Asynchronous method for async operations
   - Both methods check for existing admin to prevent duplicate creation

### 2. **Database Context** (`Tibr.Infrastructure/Contexts/ApplicationDbContext.cs`)
   - `SeedSuperAdmin()` method is called during `OnModelCreating()`
   - Ensures super admin is created when DbContext is initialized

### 3. **SeedExtensions** (`Tibr.Infrastructure/SeedExtensions.cs`)
   - Provides extension methods for `IApplicationBuilder`
   - `SeedDatabase()` - Synchronous seeding at application startup
   - `SeedDatabaseAsync()` - Asynchronous seeding at application startup
   - Ensures database is created before seeding

### 4. **API Startup** (`Tibr.API/Program.cs`)
   - Calls `app.SeedDatabase()` immediately after building the application
   - Runs before middleware configuration
   - Ensures super admin exists before accepting requests

## Seeding Flow

```
Application Startup
    ↓
Build WebApplication
    ↓
Call app.SeedDatabase()
    ↓
Create DbContext Scope
    ↓
Ensure Database Created
    ↓
SeedData.SeedSuperAdmin() called
    ↓
Check if admin exists
    ↓
If not exists:
  - Create User record with hashed password
  - Create Admin record
  - Save to database
    ↓
Application Ready
```

## Features

✅ **Idempotent** - Safe to run multiple times; checks prevent duplicates
✅ **Automatic** - Runs on application startup without manual intervention
✅ **Secure** - Password is hashed using BCrypt
✅ **Pre-verified** - OTP is pre-verified and KYC is marked as verified
✅ **Async Support** - Both sync and async seeding methods available

## Usage

### Running the Application

When you start the API or application, the super admin will be automatically created if it doesn't exist.

```bash
dotnet run
```

### Accessing Admin Panel

1. Navigate to the admin login page
2. Enter credentials:
   - **Email**: `admin@tibr.com`
   - **Password**: `SuperAdmin@123`
3. Click "Sign In"

### From API

You can also login programmatically:

```csharp
var loginRequest = new LoginRequestData(
    Email: "admin@tibr.com",
    Password: "SuperAdmin@123",
    RememberMe: true
);

// POST to /api/admin-login
```

## Important Notes

⚠️ **Important**: This is a default super admin account for development/testing. 

- **For Production**: 
  - Change the default password immediately after deployment
  - Consider using environment variables for credentials
  - Implement additional security measures for admin accounts
  - Log all admin activities

- **Security Considerations**:
  - The password is stored in the source code (for development only)
  - In production, load credentials from secure configuration sources
  - Implement multi-factor authentication for admin accounts
  - Regularly audit admin activities

## Modifying Super Admin Credentials

To change the super admin credentials, edit `Tibr.Infrastructure/Seeds/SeedData.cs`:

```csharp
// Change the email
var superAdminUser = new User
{
    Email = "newemail@company.com",  // Change this
    // ...
};

// Change the password in this line
var superAdminPassword = BCrypt.Net.BCrypt.HashPassword("NewPassword@123");
```

Then rebuild and restart the application.

## Troubleshooting

### Super Admin Not Created

1. **Check Database Connection**: Verify your connection string in `appsettings.json`
2. **Check Database Logs**: Look for any errors during seeding
3. **Manual Deletion**: If the admin was manually deleted, restart the application

### Can't Login

1. **Verify Credentials**: Double-check email and password
2. **Check Admin Status**: Ensure admin status is "Active" in database
3. **Check User Status**: Ensure user record exists with "Active" status
4. **Verify OTP**: User should have `OtpVerified = true`

## Files Modified/Created

- ✅ `Tibr.Infrastructure/Seeds/SeedData.cs` - **NEW**
- ✅ `Tibr.Infrastructure/SeedExtensions.cs` - **NEW**
- ✅ `Tibr.Infrastructure/Contexts/ApplicationDbContext.cs` - **MODIFIED**
- ✅ `Tibr.API/Program.cs` - **MODIFIED**

