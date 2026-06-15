# Plan: Withdraw + Review Features

## TL;DR

Add `Withdraw` and `Review` entities, their EF configs + migration, DTOs, service interfaces & implementations, DI registration, and two new controllers.

---

## 1. Domain Layer (`Tibr.Domain`)

### New Enum

```csharp
// Tibr.Domain/Enums/WithdrawType.cs
namespace Tibr.Domain.Enums;
public enum WithdrawType
{
    Bank,
    EWallet
}
```

### New Entities

```csharp
// Tibr.Domain/Entities/Withdraw.cs
using Tibr.Domain.Common.Classes;
using Tibr.Domain.Enums;

namespace Tibr.Domain.Entities;
public class Withdraw : BaseEntity<long>
{
    public decimal Amount { get; set; }
    public WithdrawType Type { get; set; }
    public string Name { get; set; } = string.Empty;   // bank name or wallet provider
    public string Number { get; set; } = string.Empty; // IBAN or phone/IPA
    public long UserId { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}
```

```csharp
// Tibr.Domain/Entities/Review.cs
using Tibr.Domain.Common.Classes;

namespace Tibr.Domain.Entities;
public class Review : BaseEntity<long>
{
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }  // 1–5 rating

    // Navigation
    public virtual Order Order { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
```

### Update `User.cs` — Add navigation collections

```csharp
public ICollection<Withdraw> Withdraws { get; set; } = new List<Withdraw>();
public ICollection<Review> Reviews { get; set; } = new List<Review>();
```

### Update `Order.cs` — Add navigation collection (optional but nice)

```csharp
public virtual ICollection<Review> Reviews { get; set; } = [];
```

---

## 2. Infrastructure Layer — EF Config + Migration

### ApplicationDbContext additions

```csharp
// Add new DbSets (group with related sets)
public DbSet<Withdraw> Withdraws { get; set; }
public DbSet<Review> Reviews { get; set; }
```

### `OnModelCreating` — Review composite unique index

```csharp
modelBuilder.Entity<Review>()
    .HasIndex(r => new { r.OrderId, r.UserId })
    .IsUnique();
```

### `OnModelCreating` — Decimal precision

```csharp
modelBuilder.Entity<Withdraw>()
    .Property(w => w.Amount)
    .HasPrecision(18, 2); // monetary precision

modelBuilder.Entity<Withdraw>()
    .Property(w => w.Type)
    .HasConversion<string>()
    .HasMaxLength(20);
```

### `OnModelCreating` — Relationships

```csharp
modelBuilder.Entity<Withdraw>()
    .HasOne<User>()
    .WithMany(u => u.Withdraws)
    .HasForeignKey(w => w.UserId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Review>()
    .HasOne(r => r.Order)
    .WithMany(o => o.Reviews)
    .HasForeignKey(r => r.OrderId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<Review>()
    .HasOne(r => r.User)
    .WithMany(u => u.Reviews)
    .HasForeignKey(r => r.UserId)
    .OnDelete(DeleteBehavior.Restrict);
```

### Migration

```bash
dotnet ef migrations add AddWithdrawAndReview --project Tibr.Infrastructure --startup-project Tibr.API --context ApplicationDbContext
dotnet ef database update --project Tibr.Infrastructure --startup-project Tibr.API --context ApplicationDbContext
```

---

## 3. Application Layer — DTOs

```csharp
// Tibr.Application/Dtos/WithdrawDtos.cs
using Tibr.Domain.Enums;

namespace Tibr.Application.Dtos;
public class CreateWithdrawDto
{
    public decimal Amount { get; set; }
    public WithdrawType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}

public class WithdrawDto
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

```csharp
// Tibr.Application/Dtos/ReviewDtos.cs
namespace Tibr.Application.Dtos;
public class CreateReviewDto
{
    public long OrderId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
}

public class UpdateReviewDto
{
    public string? Description { get; set; }
    public int? Value { get; set; }
}

public class ReviewDto
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string? Description { get; set; }
    public int Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

---

## 4. Application Layer — Service Interfaces

```csharp
// Tibr.Application/Services/WithdrawServices/IWithdrawService.cs
using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public interface IWithdrawService
{
    Task<Result> CreateAsync(CreateWithdrawDto dto, long userId);
}
```

```csharp
// Tibr.Application/Services/ReviewServices/IReviewService.cs
using Tibr.Application.Dtos;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ReviewServices;
public interface IReviewService
{
    Task<Result> CreateAsync(CreateReviewDto dto, long userId);
    Task<Result> UpdateAsync(long reviewId, UpdateReviewDto dto, long userId);
    Task<Result<List<ReviewDto>>> GetByUserIdAsync(long userId);
}
```

---

## 5. Application Layer — Concrete Services

### `WithdrawService`

```csharp
// Tibr.Application/Services/WithdrawServices/WithdrawService.cs
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.WithdrawServices;
public class WithdrawService : IWithdrawService
{
    private readonly IGenericRepository<Withdraw, long> _withdrawRepo;

    public WithdrawService(IGenericRepository<Withdraw, long> withdrawRepo)
    {
        _withdrawRepo = withdrawRepo;
    }

    public async Task<Result> CreateAsync(CreateWithdrawDto dto, long userId)
    {
        if (dto.Amount < 100 || dto.Amount > 50000)
            return Result.Failure("Amount must be between 100 and 50,000.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            return Result.Failure("Recipient name is required.");

        if (string.IsNullOrWhiteSpace(dto.Number))
            return Result.Failure("Account/phone number is required.");

        var withdraw = new Withdraw
        {
            Amount = dto.Amount,
            Type = dto.Type,
            Name = dto.Name,
            Number = dto.Number,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };

        await _withdrawRepo.AddAsync(withdraw);
        await _withdrawRepo.SaveChangesAsync();

        return Result.Success();
    }
}
```

### `ReviewService`

```csharp
// Tibr.Application/Services/ReviewServices/ReviewService.cs
using Tibr.Application.Dtos;
using Tibr.Domain.Entities;
using Tibr.Domain.IRepositories;
using Tibr.Domain.ResultPattern;

namespace Tibr.Application.Services.ReviewServices;
public class ReviewService : IReviewService
{
    private readonly IGenericRepository<Review, long> _reviewRepo;
    private readonly IGenericRepository<Order, long> _orderRepo;

    public ReviewService(
        IGenericRepository<Review, long> reviewRepo,
        IGenericRepository<Order, long> orderRepo)
    {
        _reviewRepo = reviewRepo;
        _orderRepo = orderRepo;
    }

    public async Task<Result> CreateAsync(CreateReviewDto dto, long userId)
    {
        if (dto.Value < 1 || dto.Value > 5)
            return Result.Failure("Value must be between 1 and 5.");

        // Check uniqueness at service level (DB unique index is the real guard)
        var exists = _reviewRepo
            .GetAll(r => r.OrderId == dto.OrderId && r.UserId == userId)
            .Any();

        if (exists)
            return Result.Failure("You have already reviewed this order.");

        // Verify the order exists and belongs to the user
        var orderExists = _orderRepo
            .GetAll(o => o.Id == dto.OrderId && o.UserId == userId)
            .Any();
        if (!orderExists)
            return Result.Failure("Order not found or does not belong to you.");

        var review = new Review
        {
            OrderId = dto.OrderId,
            UserId = userId,
            Description = dto.Description,
            Value = dto.Value,
            CreatedAt = DateTime.UtcNow,
        };

        await _reviewRepo.AddAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> UpdateAsync(long reviewId, UpdateReviewDto dto, long userId)
    {
        if (dto.Description is null && dto.Value is null)
            return Result.Failure("Nothing to update. Provide Description, Value, or both.");

        var review = await _reviewRepo.GetByIdAsync(reviewId);
        if (review is null)
            return Result.Failure("Review not found.");

        if (review.UserId != userId)
            return Result.Failure("You can only edit your own reviews.");

        if (dto.Description is not null)
            review.Description = dto.Description;

        if (dto.Value.HasValue)
        {
            if (dto.Value < 1 || dto.Value > 5)
                return Result.Failure("Value must be between 1 and 5.");
            review.Value = dto.Value.Value;
        }

        review.UpdatedAt = DateTime.UtcNow;

        await _reviewRepo.UpdateAsync(review);
        await _reviewRepo.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<List<ReviewDto>>> GetByUserIdAsync(long userId)
    {
        var reviews = _reviewRepo.GetAll(r => r.UserId == userId).ToList();

        var dtos = reviews.Select(r => new ReviewDto
        {
            Id = r.Id,
            OrderId = r.OrderId,
            Description = r.Description,
            Value = r.Value,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        }).ToList();

        return Result<List<ReviewDto>>.Success(dtos);
    }
}
```

> **Note:** `ReviewService` uses `IGenericRepository<Order, long>` for cross-entity queries. The `GetAll(...).Any()` pattern works because `GetAll` returns `IQueryable<TEntity>` — LINQ executes the query server-side. No `ApplicationDbContext` needed, preserving the Clean Architecture boundary (Application only depends on Domain).

---

## 6. DI Registration

### `Tibr.Application/DependencyInjection.cs` — add:

```csharp
using Tibr.Application.Services.WithdrawServices;
using Tibr.Application.Services.ReviewServices;

// Inside AddApplicationServices:
services.AddScoped<IWithdrawService, WithdrawService>();
services.AddScoped<IReviewService, ReviewService>();
```

---

## 7. API Layer — Controllers

### `WithdrawController`

```csharp
// Tibr.API/Controllers/WithdrawController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.WithdrawServices;

namespace Tibr.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WithdrawController : ControllerBase
{
    private readonly IWithdrawService _withdrawService;

    public WithdrawController(IWithdrawService withdrawService)
    {
        _withdrawService = withdrawService;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateWithdrawDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _withdrawService.CreateAsync(dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return StatusCode(201); // Created
    }

    private long? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}
```

### `ReviewController`

```csharp
// Tibr.API/Controllers/ReviewController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tibr.Application.Dtos;
using Tibr.Application.Services.ReviewServices;

namespace Tibr.API.Controllers;

[ApiController]
[Route("api/reviews")]
[Authorize]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateReviewDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.CreateAsync(dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage);

        return StatusCode(201);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateReviewDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.UpdateAsync(id, dto, userId.Value);
        if (result.IsFailure)
            return BadRequest(result.ErrorMessage); // 400 or 403

        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<ReviewDto>>> GetMyReviews()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _reviewService.GetByUserIdAsync(userId.Value);
        return Ok(result.Data);
    }

    private long? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (claim is null || !long.TryParse(claim.Value, out var userId))
            return null;
        return userId;
    }
}
```

> **Note:** `[Route("api/reviews")]` uses explicit kebab-case, matching the convention of `api/asset-price` and `api/investment-orders` for multi-word names.

---

## 8. Execution Order

| Step | Files | Verification |
|------|-------|-------------|
| 1. Enum + Entities | `WithdrawType.cs`, `Withdraw.cs`, `Review.cs` | `dotnet build` |
| 2. Update `User.cs`, `Order.cs` | Add navigation collections | `dotnet build` |
| 3. EF config | `ApplicationDbContext.cs` | `dotnet build` |
| 4. Migration | Run `dotnet ef migrations add` | Migration created |
| 5. Apply DB | Run `dotnet ef database update` | DB updated |
| 6. DTOs | `WithdrawDtos.cs`, `ReviewDtos.cs` | `dotnet build` |
| 7. Service interfaces | `IWithdrawService.cs`, `IReviewService.cs` | `dotnet build` |
| 8. Concrete services | `WithdrawService.cs`, `ReviewService.cs` | `dotnet build` |
| 9. DI registration | `Application/DependencyInjection.cs` | `dotnet build` |
| 10. Controllers | `WithdrawController.cs`, `ReviewController.cs` | `dotnet build` |
| 11. Smoke test | `dotnet run --project Tibr.API`, call endpoints | HTTP 200/201/400 |

---

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| `UserId` is `long` (not `string`) | Matches **every** existing entity (`Payment.UserId`, `Deposit.UserId`, `Order.UserId`). JWT `NameIdentifier` claim is parsed as `long`. |
| `OrderId` is `long` | `Order.Id` is `long` from `BaseEntity<long>`. |
| `WithdrawType` enum (not free string) | Consistent with `DepositStatus`, `PaymentMethod`, etc. EF stores as string via `HasConversion<string>()`. |
| `WithdrawService` only uses repository | Simple CRUD, no cross-entity queries needed. |
| `ReviewService` uses `IGenericRepository<Order, long>` for cross-entity queries | Avoids Application→Infrastructure dependency. `GetAll(...).Any()` keeps queries server-side via `IQueryable`. |
| All controller failures return 400 BadRequest | Consistent with the other 15 controllers. Error-code differentiation can be added later if `Result` grows an `ErrorCode` enum. |
| No `GetById` or `Delete` on `Review` | Not requested. Can be added later if admin moderation is needed. |
| No `userId` fallback from request body | **Security**: the JWT claim is the single source of truth. A body-provided `userId` would let users impersonate others. |

---

## Relevant Files (full paths)

- `Tibr.Domain/Enums/WithdrawType.cs`
- `Tibr.Domain/Entities/Withdraw.cs`
- `Tibr.Domain/Entities/Review.cs`
- `Tibr.Domain/Entities/User.cs` (+ navigation props)
- `Tibr.Domain/Entities/Order.cs` (+ navigation props)
- `Tibr.Infrastructure/Contexts/ApplicationDbContext.cs`
- `Tibr.Infrastructure/Migrations/` (auto-generated)
- `Tibr.Application/Dtos/WithdrawDtos.cs`
- `Tibr.Application/Dtos/ReviewDtos.cs`
- `Tibr.Application/Services/WithdrawServices/IWithdrawService.cs`
- `Tibr.Application/Services/WithdrawServices/WithdrawService.cs`
- `Tibr.Application/Services/ReviewServices/IReviewService.cs`
- `Tibr.Application/Services/ReviewServices/ReviewService.cs`
- `Tibr.Application/DependencyInjection.cs`
- `Tibr.API/Controllers/WithdrawController.cs`
- `Tibr.API/Controllers/ReviewController.cs`
