# Future Improvements (deferred from porting phase-6-controllers)

## Application Layer — Optional changes skipped during port

### SubmitKycCommand.cs — Security fix
Add `Guid.NewGuid()` prefix to uploaded KYC filenames to prevent overwrites.
**File:** `Tibr.Application/Services/Kyc/SubmitKycCommand.cs`
**Change:** `Path.GetExtension(...)` → `Guid.NewGuid() + Path.GetExtension(...)` (3 places)

### EmailService.cs — Cleanup
- Add `!` null-forgiving operators to config accesses
- Remove Arabic comments
- Minor formatting consistency
**File:** `Tibr.Application/Services/Email/EmailService.cs`

### CreateOrderDtoValidator.cs — Validation improvement
- `NotEmpty()` → `GreaterThan(0)` for `UserId` and item `ProductId`
**File:** `Tibr.Application/Dtos/Validators/CreateOrderDtoValidator.cs`

### CategoryService.cs — Return type consistency
- `DeleteCategoryAsync` returns `Result<string>` instead of `Result`
- `GetAllCategoriesAsync` uses manual projection with `ProductCount`
**Files:** `Tibr.Application/Services/CategoryServices/CategoryService.cs`, `ICategoryService.cs`

## Paymob Integration — Order ID handling

A detailed plan already exists at `.plans/paymob-integration-fixes.md` covering:

1. **Add `OrderId` to `CreatePaymentRequest`** — client specifies which order to pay
2. **Use `OrderId` as `special_reference`** — replaces random GUID so callback can identify the order
3. **Add `SpecialReference` to callback DTO** — deserialize Paymob's response
4. **Implement `ProcessCallbackAsync`** — mark order as paid, create Payment record
5. **Wire callback in `PaymentController`** — call `ProcessCallbackAsync` after HMAC verification
6. **Frontend redirect with `merchant_order_id`** — read from Paymob's query param, redirect to frontend order page
7. **Remove unused `ApiKey`** — dead config cleanup

The Paymob flow after fix:
```
Frontend                       API                              Paymob
  │                              │                                 │
  ├── POST /api/orders ──────────┤                                 │
  │         ← OrderDto (id=5)    │                                 │
  │                              │                                 │
  ├── POST /api/payment/initiate ─┤                                 │
  │    { orderId: 5, ... }       │                                 │
  │                              ├── Intention API ────────────────┤
  │                              │   special_reference = "5"       │
  │                              │← checkout URL                   │
  │         ← { paymentUrl }     │                                 │
  │                              │                                 │
  ├── browser redirect to Paymob ─┼────────────────────────────────┤
  │                              │                                 │
  │                              │◄── POST callback/processed ─────┤
  │                              │    special_reference = "5"      │
  │                              │    success = true               │
  │                              │                                 │
  │                              ├─ Order.PaymentStatus = "Paid"   │
  │                              ├─ Payment record created         │
  │                              │← 200 OK                         │
  │                              │                                 │
  │                              │◄── GET /callback/response ──────┤
  │                              │    ?success=true&               │
  │                              │    merchant_order_id=5          │
  │                              │                                 │
  │         ← 302 Redirect       │                                 │
  │    /orders/5?payment=success  │                                 │
```
