## Goal: Document API response schemas in OpenAPI spec

The built-in OpenAPI at `GET /openapi/v1.json` already documents request DTOs (via `[FromBody]` parameters), but response schemas were missing because all controllers returned `IActionResult` without type info.

### Changes

| File | Change |
|------|--------|
| `Tibr.Application/Dtos/Paymob/PaymentInitiateResponse.cs` | **New** — `record PaymentInitiateResponse(string PaymentUrl)` |
| `Tibr.API/Controllers/OrdersController.cs` | `IActionResult` → `ActionResult<T>` on all 6 endpoints |
| `Tibr.API/Controllers/PaymentController.cs` | `IActionResult` → `ActionResult<PaymentInitiateResponse>` on Initiate; `ActionResult` on Callback/ResponseCallback |

### Endpoint → Response Type mapping

| Endpoint | Response Type |
|----------|--------------|
| `GET /api/orders` | `IEnumerable<OrderDto>` |
| `GET /api/orders/{id}` | `OrderDto` |
| `GET /api/orders/user/{userId}` | `IEnumerable<OrderDto>` |
| `POST /api/orders` | `OrderDto` (201) |
| `PUT /api/orders/{id}` | `OrderDto` |
| `DELETE /api/orders/{id}` | No Content (204) |
| `POST /api/payment/initiate` | `PaymentInitiateResponse` |
| `POST /api/payment/callback/processed` | No Content (200/401) |
| `GET /api/payment/callback/response` | Redirect (302) |

### Verification

Start the API and visit `http://localhost:5151/openapi/v1.json` — response schemas should now appear under `components.schemas`:

```json
"OrderDto": { ... },
"PaymentInitiateResponse": { "properties": { "paymentUrl": { "type": "string" } } }
```
