## Plan: User Profile Endpoints (Get, Update, Change Password)

TL;DR — Add three `[Authorize]` endpoints under `api/auth/` following the existing MediatR pattern: GET profile data, PUT update profile fields, PUT change password.

---

### Endpoints

| Method | Route | Auth | Request | Response |
|---|---|---|---|---|
| `GET` | `api/auth/profile` | `[Authorize]` | — | `UserProfileDto` |
| `PUT` | `api/auth/profile` | `[Authorize]` | `UpdateProfileDto` | `AuthResponse` (or `UserProfileDto`) |
| `PUT` | `api/auth/change-password` | `[Authorize]` | `ChangePasswordDto` | `AuthResponse` |

---

### DTOs

In `Tibr.Application/Dtos/AuthModels.cs` (or a new `ProfileDtos.cs`):

```csharp
public class UserProfileDto
{
    public long Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string KycStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Phone { get; set; }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmNewPassword { get; set; }
}
```

**Not updating `Email`** — email is the identity key used for login, OTP, and KYC verification. Changing it would require reverification (scope for later). If you want email change included, we can add it with an OTP re-verification step.

**Not updating `KycStatus`** — managed by admin review process.

---

### MediatR Commands (new files in `Tibr.Application/Services/Auth/`)

| File | Command | Handler Logic |
|---|---|---|
| `GetProfileQuery.cs` | `GetProfileQuery(long UserId) : IRequest<UserProfileDto>` | `_context.Set<User>().FindAsync(UserId)` → map to `UserProfileDto` |
| `UpdateProfileCommand.cs` | `UpdateProfileCommand(long UserId, UpdateProfileDto Data) : IRequest<AuthResponse>` | Find user → update `FirstName`, `LastName`, `Phone` → `SaveChangesAsync` → return success |
| `ChangePasswordCommand.cs` | `ChangePasswordCommand(long UserId, ChangePasswordDto Data) : IRequest<AuthResponse>` | Find user → `BCrypt.Verify(OldPassword, user.Password)` → hash `NewPassword` → update → `SaveChangesAsync` → return success |

---

### Controller Changes

Add three actions to `AuthController.cs` (follows existing pattern — inject `IMediator`, extract userId from JWT):

```csharp
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var result = await _mediator.Send(new GetProfileQuery(userId.Value));
    return Ok(result);
}

[HttpPut("profile")]
public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
{
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var result = await _mediator.Send(new UpdateProfileCommand(userId.Value, dto));
    if (!result.IsSuccess) return BadRequest(result);
    return Ok(result);
}

[HttpPut("change-password")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
{
    var userId = GetUserId();
    if (userId is null) return Unauthorized();
    var result = await _mediator.Send(new ChangePasswordCommand(userId.Value, dto));
    if (!result.IsSuccess) return BadRequest(result);
    return Ok(result);
}
```

And add the `GetUserId()` helper (identical pattern to `DepositController`):
```csharp
private long? GetUserId()
{
    var claim = User.FindFirst(ClaimTypes.NameIdentifier);
    if (claim is null || !long.TryParse(claim.Value, out var userId))
        return null;
    return userId;
}
```

---

### Validation

- `ChangePasswordDto`: `NewPassword == ConfirmNewPassword` (in handler, before hashing)
- `ChangePasswordDto`: `OldPassword != NewPassword` (prevent reusing same password)
- `UpdateProfileDto`: `FirstName`/`LastName` not empty, `Phone` not empty (basic, can be extended)
- `ChangePasswordDto`: `NewPassword` minimum length? Existing register doesn't enforce this, so skip unless you want to add it.

---

### Relevant Files

| File | Change |
|---|---|
| `Tibr.Application/Dtos/AuthModels.cs` | Add `UserProfileDto`, `UpdateProfileDto`, `ChangePasswordDto` |
| `Tibr.Application/Services/Auth/GetProfileQuery.cs` | New — MediatR query + handler |
| `Tibr.Application/Services/Auth/UpdateProfileCommand.cs` | New — MediatR command + handler |
| `Tibr.Application/Services/Auth/ChangePasswordCommand.cs` | New — MediatR command + handler |
| `Tibr.API/Controllers/AuthController.cs` | Add 3 endpoints + `GetUserId()` helper |

No DI changes needed — MediatR auto-discovers handlers from the assembly containing `RegisterCommand` (already registered in `Program.cs`).

---

### Verification

1. Build succeeds
2. All 177 existing tests pass
3. `GET /api/auth/profile` returns `UserProfileDto` with correct data (requires JWT)
4. `PUT /api/auth/profile` updates `FirstName`, `LastName`, `Phone`
5. `PUT /api/auth/change-password` with correct old password succeeds, with wrong old password fails
6. `PUT /api/auth/change-password` with mismatched new passwords fails
7. Unauthorized requests (no JWT) return 401
