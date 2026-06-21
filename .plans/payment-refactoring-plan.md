# Plan: Refactor Payment Flow to Clean Architecture

## Analysis

### Current problems

1. **`IPaymobService` leaks Paymob into Application** — Interface in `Tibr.Application` exposes Paymob-specific types (`PaymobCallbackPayload`) and methods (`VerifyCallback`).

2. **`PaymobService` does business logic in Infrastructure** — `ProcessCallbackAsync()` directly updates `Order`, creates `Payment` records, uses repositories.

3. **Fragile in-memory maps** — `_paymobOrderMap` / `_paymobDepositMap` (`ConcurrentDictionary`) lost on app restart.

4. **Inconsistent `special_reference`** — Order payment uses `orderId.ToString()`, deposit uses `"deposit-{userId}-{guid}"`. Controller routes via `StartsWith("deposit-")` magic string.

5. **`PaymentController` not thin** — Orchestrates both flows directly, knows about Paymob HMAC.

6. **Deposit billing data hardcoded** — `DepositService.cs:59`: `"Deposit"`, `"User"`, `""`, `"0000000000"`.

### Entities involved in each flow

| Entity | Order payment | Deposit | Created/Updated by |
|--------|-------------|---------|-------------------|
| **Order** + **OrderItem** | ✅ Created at start | ❌ | `OrderService` (Application) |
| **Payment** | ✅ Created (Pending), updated (Completed) | ❌ | `PaymentService` (Application) |
| **Deposit** | ❌ | ✅ Created (Pending), updated (Completed/Failed) | `DepositService` (Application) |
| **Wallet** (Cash) | ❌ | ✅ Balance increased on success | `WalletService.CreditAsync()` (Application) |
| **WalletTransaction** | ❌ | ✅ Created (Type=Credit, ReferenceType=Deposit) | Inside `WalletService.CreditAsync()` (Application) |
| **User** | ❌ (billing sent by frontend) | ✅ Fetched from DB for real billing data | `DepositService` (Application) |
| **Reservation** | ❌ | ❌ | Separate investment flow |
| **Transaction** (Trade) | ❌ | ❌ | Separate trade flow |

## Target Architecture

```
┌──────────────────────────────────────────────────────────────┐
│  API Layer                                                   │
│  ┌──────────────────┐  ┌──────────────────┐                  │
│  │ PaymentController │  │ DepositController│                  │
│  │  (thin, delegates)│  │  (unchanged)     │                  │
│  └──────┬───────────┘  └────────┬─────────┘                  │
└─────────┼───────────────────────┼────────────────────────────┘
          │                       │
┌─────────┼───────────────────────┼────────────────────────────┐
│  App    ▼                       ▼                            │
│  ┌─────────────────────┐  ┌──────────────┐                  │
│  │   PaymentService    │  │ DepositService│                  │
│  │  (orchestration)    │  │  (unchanged   │                  │
│  │  - initiate order   │  │   contract)   │                  │
│  │  - handle callback  │  │               │                  │
│  │  - route to deposit │  │               │                  │
│  └──────────┬──────────┘  └──────┬───────┘                  │
│             │                    │                            │
│  ┌──────────▼────────────────────▼──────────────────────┐    │
│  │         IPaymentGateway (interface)                  │    │
│  │  CreateIntentionAsync(request) -> result             │    │
│  │  VerifyWebhook(rawBody, signature) -> bool           │    │
│  │  ExtractWebhookData(rawBody) -> PaymentWebhookData   │    │
│  └──────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────▼────────────────────────────────┐
│  Infrastructure                                               │
│  ┌──────────────────────────────┐                             │
│  │  PaymobPaymentGateway        │                             │
│  │  - pure HTTP + HMAC only     │                             │
│  │  - NO repositories           │                             │
│  │  - NO business logic         │                             │
│  └──────────────────────────────┘                             │
└──────────────────────────────────────────────────────────────┘
```

## New files

| File | Purpose |
|------|---------|
| `Tibr.Application/Services/PaymentServices/IPaymentGateway.cs` | Provider-agnostic payment interface |
| `Tibr.Application/Services/PaymentServices/PaymentService.cs` | Orchestrates order payment + routes callbacks |
| `Tibr.Application/Dtos/Payment/PaymentIntentionRequest.cs` | Agnostic DTO for initiating payment |
| `Tibr.Application/Dtos/Payment/PaymentInitiationResult.cs` | Agnostic result DTO |
| `Tibr.Application/Dtos/Payment/PaymentWebhookData.cs` | Agnostic parsed webhook data |
| `Tibr.Infrastructure/Services/PaymobPaymentGateway.cs` | Replaces `PaymobService` |

## Modified files

| File | What changes |
|------|-------------|
| `PaymobService.cs` → **delete** | Replaced by `PaymobPaymentGateway` |
| `IPaymobService.cs` → **delete** | Replaced by `IPaymentGateway` |
| `PaymobCallbackPayload.cs` → **move to Infrastructure** | Paymob-specific, no longer in Application |
| `CreatePaymentRequest.cs` | Keep, still used by order initiation endpoint |
| `DepositService.cs` | Change `_paymobService` → `_paymentGateway`, fetch user from DB for billing |
| `PaymentController.cs` | Inject `PaymentService` instead of `IPaymobService` + `IDepositService` |
| `Program.cs` | Update DI registrations |
| `appsettings.json` | No changes expected |

## Unchanged (frontend contract)

- `InitiateDepositDto` (Amount, PaymentMethod)
- `CreateOrderDto` (UserId, Items)
- `CreatePaymentRequest` (OrderId, billing fields)
- `OrdersController`
- `DepositController`
- All cart logic
- Order flow: frontend creates order first → then initiates payment

## Key design decisions

| Topic | Decision |
|-------|----------|
| **Gateway interface** | `IPaymentGateway` in Application — provider-agnostic |
| **Webhook verify** | Separate `VerifyWebhook()` + `ExtractWebhookData()` (Option B) |
| **`special_reference` format (order)** | `"payment:{paymentId}:{timestamp}"` — `Payment` record pre-created (Pending) |
| **`special_reference` format (deposit)** | `"deposit:{depositId}:{timestamp}"` — `Deposit` record created first |
| **Order double-pay guard** | Check for Completed → `"already paid"`. Check for Pending → `"payment in progress"`. |
| **Deposit billing data** | Fetch real user info from DB instead of hardcoded values |
| **PaymobService** | Stripped of business logic — only HTTP + HMAC, no repositories |

## special_reference

```
Order (retry):   payment:5:1717000000   → Payment(id=5, Pending), first attempt, timestamp
                 payment:5:1717000100   → retry after failure, same Payment, new timestamp

Deposit:         deposit:42:1717000000  → Deposit(id=42, Pending), unique per deposit
```

Parsed on callback: split on `:` → `[entityType, entityId, timestamp]`.

## Flows

### Order payment

```
Frontend                        API                                    Paymob
  │                              │                                        │
  ├─ POST /api/orders ───────────┤                                        │
  │     { userId, items }        │  (unchanged — OrdersController)        │
  │← { id: 5, ... }              │                                        │
  │                              │                                        │
  ├─ POST /api/payment/initiate ─┤                                        │
  │     { orderId: 5, billing }  │                                        │
  │                              ├─ PaymentService.InitiateOrderAsync()    │
  │                              │   ├── Check: order exists?              │
  │                              │   ├── Check: any Completed Payment?     │
  │                              │   │     → return "already paid"         │
  │                              │   ├── Check: any Pending Payment?       │
  │                              │   │     → return "payment in progress"  │
  │                              │   ├── Create Payment(Pending, orderId)  │
  │                              │   ├── Build PaymentIntentionRequest     │
  │                              │   ├── _gateway.CreateIntentionAsync() ──┤
  │                              │   │     special_reference =             │
  │                              │   │     "payment:{id}:{timestamp}"      │
  │                              │   │← checkout URL ←─────────────────────┤
  │                              │   ├── (on failure: mark Payment Failed) │
  │                              │← { paymentUrl }                         │
  │← { paymentUrl }              │                                        │
  │                              │                                        │
  │ (user pays on Paymob)        │                                        │
  │                              │                                        │
  │                              │◄─ POST callback/processed ─────────────┤
  │                              │    raw body + ?hmac=...                 │
  │                              │                                        │
  │                              ├─ PaymentService.HandleCallback()        │
  │                              │   ├── _gateway.VerifyWebhook(body,hmac) │
  │                              │   ├── _gateway.ExtractWebhookData(body) │
  │                              │   ├── Parse special_reference           │
  │                              │   ├── "payment" → find Payment by id    │
  │                              │   ├── Update Payment → Completed        │
  │                              │   ├── Update Order.PaymentStatus=Paid   │
  │                              │   ├── Order.OrderStatus=Processing      │
  │                              │← 200 OK                                │
  │                              │                                        │
  │                              │◄─ GET callback/response ───────────────┤
  │                              │    ?success=true&merchant_order_id=...  │
  │← 302 → /orders/5?payment=success                                      │
```

### Deposit

```
Frontend                        API                                    Paymob
  │                              │                                        │
  ├─ POST /api/deposit/initiate ─┤                                        │
  │     { amount, paymentMethod }│  (unchanged contract)                  │
  │                              │                                        │
  │                              ├─ DepositService.InitiateAsync()        │
  │                              │   ├── Fetch user from DB (real billing)│
  │                              │   ├── Create Deposit(Pending)          │
  │                              │   ├── Build PaymentIntentionRequest    │
  │                              │   ├── _gateway.CreateIntentionAsync() ──┤
  │                              │   │     special_reference =             │
  │                              │   │     "deposit:{depositId}:{ts}"     │
  │                              │   │← checkout URL ←─────────────────────┤
  │                              │   ├── (on failure: mark Deposit Failed)│
  │                              │← { checkoutUrl }                       │
  │← { checkoutUrl }             │                                        │
  │                              │                                        │
  │ (user pays on Paymob)        │                                        │
  │                              │                                        │
  │                              │◄─ POST callback/processed ─────────────┤
  │                              │    raw body + ?hmac=...                 │
  │                              │                                        │
  │                              ├─ PaymentService.HandleCallback()        │
  │                              │   ├── _gateway.VerifyWebhook(body,hmac) │
  │                              │   ├── _gateway.ExtractWebhookData(body) │
  │                              │   ├── Parse "deposit:{id}:{ts}"        │
  │                              │   ├── delegate to DepositService       │
  │                              │   │   .HandleCallbackAsync(depositId,  │
  │                              │   │    success)                        │
  │                              │   │                                    │
  │                              │   │   [DepositService: if success]     │
  │                              │   │   ├── Find Deposit by id           │
  │                              │   │   ├── Idempotency: skip if         │
  │                              │   │   │   already Completed            │
  │                              │   │   ├── Deposit.Status = Completed   │
  │                              │   │   ├── Find user's Cash Wallet      │
  │                              │   │   ├── WalletService.CreditAsync(   │
  │                              │   │   │   walletId, amount,            │
  │                              │   │   │   ReferenceType.Deposit,       │
  │                              │   │   │   deposit.Id)                  │
  │                              │   │   │   ├── wallet.Balance += amount │
  │                              │   │   │   └── WalletTransaction(       │
  │                              │   │   │       Type=Credit,             │
  │                              │   │   │       ReferenceType=Deposit)   │
  │                              │   │                                    │
  │                              │   │   [DepositService: if failed]      │
  │                              │   │   └── Deposit.Status = Failed      │
  │                              │   │                                    │
  │                              │← 200 OK                                │
```

## Implementation order

1. Create `IPaymentGateway` interface + DTOs in Application
2. Create `PaymobPaymentGateway` in Infrastructure (bare HTTP + HMAC, no repos)
3. Create `PaymentService` in Application (orchestration + routing)
4. Update `DepositService` (switch to `IPaymentGateway`, fetch user billing)
5. Update `PaymentController` (thin, use `PaymentService`)
6. Update DI registrations in `Program.cs`
7. Delete old `IPaymobService`, `PaymobService`
8. Move `PaymobCallbackPayload` to Infrastructure
