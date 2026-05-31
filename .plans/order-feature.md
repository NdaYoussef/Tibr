## Plan: Order Feature Implementation

**Tech decisions:**
- Mapping: Mapster (with IRegister profiles)
- TotalAmount: server-calculated (sum of Quantity × Price per line item)
- OrderNumber: GUID-based (`ORD-{Guid}`)
- Validation: FluentValidation (with auto-pipeline)

---

### Phase 1 — Domain: `IGenericRepository<T>`

**File:** `Tibr.Domain/IRepositories/IGenericRepository.cs`

Define `IGenericRepository<TEntity>` constrained to `BaseEntity<long>` with basic CRUD methods only. No include overloads — complex queries with `Include`/`ThenInclude` are handled by dedicated query services (see Phase 3).

**File:** `Tibr.Infrastructure/Repositories/GenericRepository.cs`
- `GenericRepository<TEntity> : IGenericRepository<TEntity>` where `TEntity : BaseEntity<long>`
- Inject `ApplicationDbContext`, use internal `DbSet<TEntity>`
- All reads filter `!IsDeleted`; `DeleteAsync` does soft delete

**File:** `Tibr.Infrastructure/DependencyInjection.cs`
- Replace stub with `public static class DependencyInjection`
- `AddInfrastructureServices(this IServiceCollection)` registers `IGenericRepository<>`

---

### Phase 3 — Application: DTOs + Mapster + FluentValidation + Service

**File:** `Tibr.Application/Tibr.Application.csproj`
- Project ref to `Tibr.Domain`
- NuGet: `Mapster`, `Mapster.DependencyInjection`, `FluentValidation.AspNetCore`

**File:** `Tibr.Application/Dtos/OrderDtos.cs`
| DTO | Properties |
|-----|-----------|
| `OrderDto` | `Id`, `UserId`, `UserFullName`, `OrderNumber`, `TotalAmount`, `PaymentStatus`, `OrderStatus`, `CreatedAt`, `Items: List<OrderItemDto>` |
| `CreateOrderDto` | `UserId`, `Items: List<CreateOrderItemDto>` |
| `UpdateOrderDto` | `PaymentStatus`, `OrderStatus` |
| `OrderItemDto` | `Id`, `ProductId`, `ProductName`, `Quantity`, `Price` |
| `CreateOrderItemDto` | `ProductId`, `Quantity` |

**File:** `Tibr.Application/Dtos/Validators/CreateOrderDtoValidator.cs`
- FluentValidation: `UserId > 0`, `Items` not empty, each item `ProductId > 0` + `Quantity` 1–10000

**File:** `Tibr.Application/Dtos/Validators/UpdateOrderDtoValidator.cs`
- FluentValidation: at least one of `PaymentStatus` / `OrderStatus` must be provided

**File:** `Tibr.Application/Mappers/OrderMappingConfig.cs`
- Mapster `IRegister`: `Order → OrderDto`, `OrderItem → OrderItemDto`

**File:** `Tibr.Application/Services/IOrderService.cs`
- `GetByIdAsync`, `GetAllAsync`, `GetByUserIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
- All return `Result<T>` / `Result`

**File:** `Tibr.Application/InfrastructureContracts/IOrderQueryService.cs`
- Interface for reading Orders with eagerly-loaded navigation properties (User, OrderItems, Product)

**File:** `Tibr.Infrastructure/Queries/OrderQueryService.cs`
- Implements `IOrderQueryService` using EF Core `Include` / `ThenInclude`
- Three methods: `GetByIdWithDetailsAsync`, `GetAllWithDetailsAsync`, `GetByUserIdWithDetailsAsync`
- Keeps EF Core concerns out of Application layer

**File:** `Tibr.Infrastructure/DependencyInjection.cs`
- Registers `IOrderQueryService → OrderQueryService`

**File:** `Tibr.Application/Services/OrderService.cs`
- Injects `IGenericRepository<Order>`, `IGenericRepository<OrderItem>`, `IGenericRepository<Product>`, `IOrderQueryService`
- **Reads** use `IOrderQueryService` (with Include/ThenInclude)
- **Writes** use `IGenericRepository` (Add, Update, Delete)
- Uses `.Adapt<T>()` Mapster extension methods (no injected mapper)
- `CreateAsync`: `OrderNumber = $"ORD-{Guid.NewGuid()}"`, calculate `TotalAmount` from items, save in transaction

**File:** `Tibr.Application/DependencyInjection.cs`
- `AddApplicationServices(this IServiceCollection)` registers `IOrderService → OrderService` + `AddMapster()` + `AddFluentValidationAutoValidation()`

---

### Phase 4 — API: Controller + Wiring

**File:** `Tibr.API/Controllers/OrdersController.cs`
```
[Route("api/orders")]
```
| Method | Endpoint | Action |
|--------|----------|--------|
| GET | `/api/orders` | `GetAll()` |
| GET | `/api/orders/{id}` | `GetById(long id)` |
| GET | `/api/orders/user/{userId}` | `GetByUserId(long userId)` |
| POST | `/api/orders` | `Create(CreateOrderDto dto)` |
| PUT | `/api/orders/{id}` | `Update(long id, UpdateOrderDto dto)` |
| DELETE | `/api/orders/{id}` | `Delete(long id)` |

- Map `Result` to HTTP status codes

**File:** `Tibr.API/Program.cs`
```csharp
builder.Services.AddInfrastructureServices();
builder.Services.AddApplicationServices();
```

**File:** `Tibr.API/Tibr.API.csproj`
- Project reference to `Tibr.Application`

---

### Verification

1. `dotnet build` compiles clean
2. `POST /api/orders` with valid body → `201 Created` + order with `OrderNumber`
3. `GET /api/orders/{id}` → returns order with items
4. `PUT /api/orders/{id}` → returns updated order
5. `DELETE /api/orders/{id}` → `204 No Content`
