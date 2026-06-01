## Plan: Connect Paymob to Order Feature

### Current problems

1. `CreatePaymentRequest` has no `OrderId` — client can't tell the server which order is being paid
2. `special_reference` is a random GUID — no connection between Paymob transaction and our Order
3. Callback does nothing — `// TODO` on line 63, no `Payment` record or `Order.PaymentStatus` update
4. `PaymobOrder` DTO missing `SpecialReference` — can't read back our order reference from the callback
5. `Payment` entity is orphaned — never created, never saved
6. `ApiKey` in settings is unused — only `SecretKey` is used for auth
7. Redirect URLs are hardcoded — `https://yourapp.com/payment/...` literal strings

---

### Step 1 — Add `OrderId` to `CreatePaymentRequest`

**File:** `Tibr.Application/Dtos/Paymob/CreatePaymentRequest.cs`

Add `public long OrderId { get; set; }` so the client can specify which order is being paid.

**Reason:** The API needs to know which order to mark as paid when the callback returns successfully.

---

### Step 2 — Use `OrderId` as `special_reference` in PaymobService

**File:** `Tibr.Infrastructure/Services/PaymobService.cs`

Change:
```csharp
// Before:
special_reference = Guid.NewGuid().ToString(),

// After:
special_reference = request.OrderId.ToString(),
```

**Reason:** Paymob's callback sends back `special_reference` with the transaction details, allowing the callback handler to identify which order was paid.

---

### Step 3 — Add `SpecialReference` to callback DTO

**File:** `Tibr.Application/Dtos/Paymob/PaymobCallbackPayload.cs`

Add to `PaymobOrder`:
```csharp
[JsonPropertyName("special_reference")]
public string? SpecialReference { get; set; }
```

**Reason:** The `TransactionObject.Order` in the callback payload contains `special_reference`. The DTO must deserialize it so the callback handler can read the Order ID.

---

### Step 4 — Add `ProcessCallbackAsync` to `PaymobService`

**File:** `Tibr.Application/Services/IPaymobService.cs`

Add to interface:
```csharp
Task ProcessCallbackAsync(PaymobCallbackPayload payload);
```

**File:** `Tibr.Infrastructure/Services/PaymobService.cs`

Inject `IGenericRepository<Order>`, `IGenericRepository<Payment>`, `ILogger<PaymobService>` into the constructor.
Add `ProcessCallbackAsync` implementation that:
- Reads `OrderId` from `transaction.Order.SpecialReference`
- Fetches the `Order` by ID
- Sets `Order.PaymentStatus = "Paid"`
- Creates a `Payment` record (OrderId, UserId, Amount, PaymentMethod, Status, PaidAt)

**Reason:** Persistence logic lives in the existing PaymobService since it's the natural home for all Paymob-related operations. No new service needed.

---

### Step 5 — Wire the callback in `PaymentController`

**File:** `Tibr.API/Controllers/PaymentController.cs`

Replace the entire callback body with a single call to `_paymob.ProcessCallbackAsync(payload)` after HMAC verification passes. Inject `IOptions<PaymobSettings>` for configurable redirect URLs.

**Reason:** The callback is the webhook that confirms payment. Without this, successful payments are never recorded.

---

### Step 6 — Frontend redirect with order ID

**Problem:** Paymob's response redirect (`/api/Payment/callback/response`) only had `?success=true` — no order ID. The frontend couldn't know which order was paid.

**Discovery:** Logged the actual Paymob redirect query string and found `merchant_order_id=<our OrderId>` is included (it mirrors whatever we set as `special_reference`).

**Solution:**

**File:** `Tibr.Infrastructure/Config/PaymobSettings.cs`

Replace the two redirect URLs with a single `FrontendBaseUrl`:
```csharp
public string FrontendBaseUrl { get; set; } = "http://localhost:4200";
```

**File:** `Tibr.API/Controllers/PaymentController.cs`

Read `merchant_order_id` from the query and build the URL:
```csharp
var orderId = Request.Query["merchant_order_id"];
var status = success ? "success" : "failed";
var redirectUrl = $"{_settings.FrontendBaseUrl}/orders/{orderId}?payment={status}";
```

**File:** `Tibr.API/appsettings.json`
```json
"FrontendBaseUrl": "http://localhost:4200"
```

**Result:** The browser is redirected to `http://localhost:4200/orders/20?payment=success` — frontend knows exactly which order was paid.

**Reason:** A single `FrontendBaseUrl` is simpler than two redirect URLs. The backend constructs the full URL with the order ID from Paymob's query param. Changing the frontend URL only requires updating `appsettings.json` — no Paymob dashboard changes.

---

### Step 7 — Remove unused `ApiKey` from settings (low priority)

**File:** `Tibr.Infrastructure/Config/PaymobSettings.cs`

Remove `ApiKey` property. It's in `appsettings.json` but never read by `PaymobService` (uses `SecretKey` instead).

**Reason:** Dead configuration is misleading.

---

### Files touched

| Layer | File | Action |
|-------|------|--------|
| Application | `Dtos/Paymob/CreatePaymentRequest.cs` | Add `OrderId` |
| Application | `Dtos/Paymob/PaymobCallbackPayload.cs` | Add `SpecialReference` to `PaymobOrder` |
| Application | `Services/IPaymobService.cs` | Add `ProcessCallbackAsync` to interface |
| Infrastructure | `Services/PaymobService.cs` | Use `request.OrderId` as `special_reference`; add repo + logger deps; add `ProcessCallbackAsync` impl |
| Infrastructure | `Config/PaymobSettings.cs` | Replace `SuccessRedirectUrl` / `FailureRedirectUrl` with `FrontendBaseUrl`, remove `ApiKey` |
| API | `Controllers/PaymentController.cs` | Inject `IOptions<PaymobSettings>`, call `ProcessCallbackAsync`, read `merchant_order_id` from query, redirect to `FrontendBaseUrl/orders/{orderId}?payment={status}` |
| API | `appsettings.json` | Replace two redirect URLs with single `FrontendBaseUrl` |
| API | `appsettings.json.example` | Same change as above |

### What happens after the fix

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
  │                              │                                 │
  │  reads orderId=5 from URL    │                                 │
  │  shows success toast         │                                 │
  │  fetches order details       │                                 │
```

### Verification

1. Create an order via `POST /api/orders`
2. Initiate payment via `POST /api/payment/initiate` with the order's ID
3. Simulate a callback with `curl` posting a mock payload with `special_reference` set to the order ID
4. `GET /api/orders/{id}` shows `paymentStatus: "Paid"`
5. The `Payments` table has a new row linked to the order
