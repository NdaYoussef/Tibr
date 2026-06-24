# Porting Plan: feature/phase-6-controllers → investment-dev-back

## Stage 1 — What was on `origin/feature/phase-6-controllers` ✅ ALL DONE

Everything that existed on the remote is already on our branch.
The remote had only minor diffs (Stock decimal→long, SupportResponse enum,
SupportService fixes) — all already present here.

## Stage 2 — What's only in the local copy (commit `92ff704`, never pushed)

**Source:** `/home/eslam-abd-elsatar/iti-content/FinalProject/Tibr/Backend/Tibr`
(local `feature/phase-6-controllers`)

**Target:** Current branch `investment-dev-back`

All investment module files exist only in this local copy.

### Phase 3: Application — ✅ ALL DONE (commit `7a524fe`)
- [x] **DTOs** (6 files): `AddressDtos`, `AssetPriceDtos`, `DeliveryDtos`, `DepositDtos`, `TradingDtos`, `WalletDtos`
- [x] **Mappers** (4 files): `AddressMappingConfig`, `AssetPriceMappingConfig`, `TradingMappingConfig`, `WalletMappingConfig`
- [x] **Services** (8 pairs interface+impl): Address, AssetPrice, Delivery, Deposit, InvestmentOrder, Resolution, Trade, Wallet
- [x] **Modified DI**: register new services
- [x] **Modified files**: merge changes to `IPaymobService.cs`, `PaymobService.cs`

### Phase 4: API — ✅ ALL DONE (commit `7a524fe`)
- [x] **7 new controllers**: AddressController, AssetPriceController, DeliveryController, DepositController, InvestmentOrderController, TradeController, WalletController
- [x] **Modified**: `PaymentController.cs` (deposit callback handling)

### Phase 5: Migration — not needed (schema unchanged in Stage 2)

---

## Decisions
- **No custom repository interfaces or classes** — all services use `IGenericRepository<,>` directly.
  GenericRepository's `GetAll(Expression<Func<TEntity, bool>>)` provides the IQueryable to
  express any query inline in the service with LINQ. Zero new Domain or Infrastructure files.
- Skip entity config files from Infrastructure — we configure inline in DbContext.
- Skip NotificationService — not in our target architecture.
- Port DTOs, Mappers, Services, and Controllers fully from local copy, refactoring service
  constructors to inject `IGenericRepository<Entity, long>` instead of custom repo types.
- Merge changes carefully for modified files (OrderService, PaymobService, DI, Program.cs, PaymentController).

## Source paths (local copy)
```
/home/eslam-abd-elsatar/iti-content/FinalProject/Tibr/Backend/Tibr/
├── Tibr.Application/
│   ├── Dtos/{Address,AssetPrice,Delivery,Deposit,Trading,Wallet}Dtos.cs
│   ├── Mappers/{Address,AssetPrice,Trading,Wallet}MappingConfig.cs
│   ├── Services/{Address,AssetPrice,Delivery,Deposit,InvestmentOrder,Resolution,Trade,Wallet}Services/
│   └── DependencyInjection.cs
├── Tibr.API/
│   ├── Controllers/{Address,AssetPrice,Delivery,Deposit,InvestmentOrder,Trade,Wallet}Controller.cs
│   ├── Controllers/PaymentController.cs
│   └── Program.cs
```
