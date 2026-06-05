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

### Phase 3: Application — 📋 needs porting
- [ ] **DTOs** (6 files): `AddressDtos`, `AssetPriceDtos`, `DeliveryDtos`, `DepositDtos`, `TradingDtos`, `WalletDtos`
- [ ] **Mappers** (4 files): `AddressMappingConfig`, `AssetPriceMappingConfig`, `TradingMappingConfig`, `WalletMappingConfig`
- [ ] **Services** (8 pairs interface+impl): Address, AssetPrice, Delivery, Deposit, InvestmentOrder, Resolution, Trade, Wallet
- [ ] **Modified DI**: register new services
- [ ] **Modified files**: merge changes to `OrderService.cs`, `IPaymobService.cs`

### Phase 4: API — 📋 needs porting
- [ ] **7 new controllers**: AddressController, AssetPriceController, DeliveryController, DepositController, InvestmentOrderController, TradeController, WalletController
- [ ] **Modified**: `PaymentController.cs`, `Program.cs`

### Phase 5: Migration — pending
- [ ] Generate fresh migration after all layers are ported
- [ ] Apply to database (if needed)

---

## Decisions
- Skip custom IRepository interfaces from Domain — our GenericRepository handles CRUD
- Skip entity config files from Infrastructure — we configure inline in DbContext
- Skip custom repositories — GenericRepository suffices
- Skip NotificationService — not in our target architecture
- Port DTOs, Mappers, Services, and Controllers fully from local copy
- Merge changes carefully for modified files (OrderService, PaymobService, DI, Program.cs, PaymentController)

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
