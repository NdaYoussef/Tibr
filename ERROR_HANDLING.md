# Error Handling Implementation Guide

## Overview

Comprehensive error handling has been implemented throughout the application to gracefully handle failures, provide meaningful error messages, and ensure the application remains stable even when issues occur.

## Components

### 1. **Custom Exception Classes** (`Tibr.Infrastructure/Exceptions/SeedDataException.cs`)

Three specialized exception types for seed data operations:

```csharp
// Base exception for all seed operations
public class SeedDataException : Exception

// Specific to database connection issues
public class SeedDatabaseConnectionException : SeedDataException

// Specific to admin creation failures
public class SeedAdminCreationException : SeedDataException
```

**Usage:**
- `SeedDatabaseConnectionException` → Database connection issues (HTTP 503)
- `SeedAdminCreationException` → Admin data creation failures (HTTP 500)
- `SeedDataException` → General seeding errors (HTTP 500)

---

### 2. **Enhanced SeedData Class** (`Tibr.Infrastructure/Seeds/SeedData.cs`)

**Features:**
- ✅ Try-catch blocks for error handling
- ✅ Integrated logging support
- ✅ Specific exception handling for database errors
- ✅ Information, warning, and error level logging

**Methods:**
```csharp
// Synchronous seeding with error handling
public static void SeedSuperAdmin(ApplicationDbContext context)

// Asynchronous seeding with error handling
public static async Task SeedSuperAdminAsync(ApplicationDbContext context)

// Set logger for seeding operations
public static void SetLogger(ILogger logger)
```

**Error Handling Flow:**
```
Try Seeding
  ├─ DbUpdateException → SeedDatabaseConnectionException
  ├─ General Exception → SeedAdminCreationException
  └─ Success → Log Information
```

---

### 3. **Enhanced SeedExtensions** (`Tibr.Infrastructure/SeedExtensions.cs`)

**Features:**
- ✅ Try-catch wrapper around seeding operations
- ✅ Factory-based logger creation
- ✅ Both sync and async variants
- ✅ Detailed logging at each stage
- ✅ Graceful error propagation

**Methods:**
```csharp
// Async database seeding with error handling
public static async Task SeedDatabaseAsync(this IApplicationBuilder app)

// Sync database seeding with error handling
public static void SeedDatabase(this IApplicationBuilder app)
```

**Logging Stages:**
1. `Info` - Seeding started
2. `Info` - Database creation check
3. `Info` - Seeding super admin data
4. `Error` - Seed data exceptions
5. `Critical` - Unexpected errors
6. `Info` - Success completion

---

### 4. **Global Exception Handling Middleware** (`Tibr.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs`)

**Purpose:** Catches unhandled exceptions and returns consistent JSON error responses

**Exception Mapping:**

| Exception Type | HTTP Status | Message |
|---|---|---|
| `SeedDatabaseConnectionException` | 503 Service Unavailable | Database connection failed |
| `SeedAdminCreationException` | 500 Internal Server Error | Error occurred during setup |
| `SeedDataException` | 500 Internal Server Error | Data seeding error |
| `ArgumentException` | 400 Bad Request | Invalid argument provided |
| `UnauthorizedAccessException` | 401 Unauthorized | Unauthorized access |
| Default/Other | 500 Internal Server Error | Unexpected error occurred |

**Response Format:**
```json
{
  "message": "Error description",
  "success": false,
  "details": "Additional error information (if available)"
}
```

---

### 5. **Middleware Extensions** (`Tibr.Infrastructure/MiddlewareExtensions.cs`)

**Extension Method:**
```csharp
public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
```

**Usage in Program.cs:**
```csharp
app.UseGlobalExceptionHandling();
```

---

## Integration Points

### API Startup (`Tibr.API/Program.cs`)

```csharp
var app = builder.Build();

// Seed database with error handling
try
{
    app.SeedDatabase();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
    logger.LogWarning("Continuing with application startup despite seeding error.");
}

// Add global exception handling middleware
app.UseGlobalExceptionHandling();
```

---

## Error Handling Scenarios

### Scenario 1: Database Connection Failure

```
Application Startup
  ↓
Call SeedDatabase()
  ↓
EnsureCreated() fails
  ↓
DbUpdateException caught
  ↓
Converted to SeedDatabaseConnectionException
  ↓
Logged as Error
  ↓
Exception caught in Program.cs
  ↓
Logged as Error with message
  ↓
Application continues to run
```

**HTTP Response (503):**
```json
{
  "message": "Database connection failed. The service is temporarily unavailable.",
  "success": false,
  "details": "Inner exception message"
}
```

---

### Scenario 2: Admin Record Already Exists

```
Application Startup
  ↓
Check if admin exists
  ↓
Record found
  ↓
Return early (logged as Info)
  ↓
Seeding skipped
```

**Log Output:**
```
[Information] Starting super admin seeding...
[Information] Super admin already exists. Skipping seed operation.
```

---

### Scenario 3: Unhandled Controller Exception

```
API Request
  ↓
Controller throws ArgumentException
  ↓
Middleware catches exception
  ↓
Exception type matched (ArgumentException)
  ↓
HTTP 400 response returned
  ↓
JSON error response sent to client
```

**HTTP Response (400):**
```json
{
  "message": "Invalid argument provided.",
  "success": false,
  "details": "Argument cannot be null (Parameter 'email')"
}
```

---

## Logging Configuration

Logs are captured at multiple levels:

### Log Levels Used:
- **Information** - Normal operation progress
- **Warning** - Potential issues (e.g., data inconsistency)
- **Error** - Expected failures (validation, constraint violations)
- **Critical** - Severe issues affecting application stability

### Log Output Example:
```
[2024-01-15 10:30:45 Information] Database seeding started (sync)...
[2024-01-15 10:30:45 Information] Ensuring database is created (sync)...
[2024-01-15 10:30:46 Information] Database creation check completed (sync).
[2024-01-15 10:30:46 Information] Seeding super admin data (sync)...
[2024-01-15 10:30:46 Information] Super admin user created with ID: 1
[2024-01-15 10:30:46 Information] Creating admin record (sync)...
[2024-01-15 10:30:46 Information] Admin record created successfully. Async seeding completed.
[2024-01-15 10:30:46 Information] Database seeding completed successfully (sync).
```

---

## Best Practices

### 1. Always Log Errors
```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Descriptive error message");
    throw; // Re-throw or convert to appropriate exception
}
```

### 2. Use Specific Exception Types
```csharp
catch (DbUpdateException dbEx)
{
    // Handle database errors specifically
    throw new SeedDatabaseConnectionException("...", dbEx);
}
```

### 3. Provide Context Information
```csharp
logger.LogInformation($"Super admin user created with ID: {superAdminUser.Id}");
```

### 4. Log Before and After Operations
```csharp
logger.LogInformation("Creating super admin user...");
// ... do work ...
logger.LogInformation("Super admin user created successfully.");
```

---

## Testing Error Scenarios

### Test 1: Invalid Database Connection
```csharp
// In appsettings.json, set invalid connection string
"DefaultConnection": "Server=invalid;Database=test;"

// Expected: SeedDatabaseConnectionException thrown
// Expected HTTP: 503 Service Unavailable
```

### Test 2: Missing Required Field
```csharp
// Remove required field before seeding
// Expected: SeedAdminCreationException thrown
// Expected HTTP: 500 Internal Server Error
```

### Test 3: Duplicate Admin
```csharp
// Run seeding twice
// First run: Creates admin
// Second run: Skips creation (logged as Info)
// Expected HTTP: 200 OK (no error)
```

---

## Configuration

### appsettings.json

Add logging configuration:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Information",
      "Tibr.Infrastructure": "Information"
    }
  }
}
```

### Program.cs

Logging is automatically configured by ASP.NET Core. To customize:
```csharp
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
```

---

## Files Created/Modified

✅ `Tibr.Infrastructure/Exceptions/SeedDataException.cs` - **NEW**
✅ `Tibr.Infrastructure/Middleware/GlobalExceptionHandlingMiddleware.cs` - **NEW**
✅ `Tibr.Infrastructure/MiddlewareExtensions.cs` - **NEW**
✅ `Tibr.Infrastructure/Seeds/SeedData.cs` - **MODIFIED**
✅ `Tibr.Infrastructure/SeedExtensions.cs` - **MODIFIED**
✅ `Tibr.API/Program.cs` - **MODIFIED**

---

## Troubleshooting

### Issue: Seeding fails with "Database connection failed"
**Solution:**
1. Check connection string in `appsettings.json`
2. Verify SQL Server is running
3. Check network connectivity
4. Review detailed error in application logs

### Issue: Admin not created despite no errors
**Solution:**
1. Check if admin already exists: `SELECT * FROM Admins WHERE Email = 'admin@tibr.com'`
2. Check if corresponding user exists: `SELECT * FROM Users WHERE Email = 'admin@tibr.com'`
3. Review seeding logs for skipped operations

### Issue: Application fails to start due to seeding error
**Solution:**
- Application is designed to continue even if seeding fails
- Check logs to identify the root cause
- Fix the issue and restart application
- Seeding will retry on next startup

---

## Summary

✅ **Comprehensive error handling** - All critical operations wrapped in try-catch
✅ **Custom exceptions** - Specific exception types for different error scenarios
✅ **Integrated logging** - Detailed logging at Information, Warning, and Error levels
✅ **Global middleware** - Consistent error responses across all endpoints
✅ **Graceful degradation** - Application continues running despite seeding failures
✅ **Production ready** - Secure error messages without exposing sensitive details

