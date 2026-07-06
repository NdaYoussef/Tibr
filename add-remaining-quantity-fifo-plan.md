# Plan: Add `RemainingQuantity` + cheapest-first sell tracking

## Problem
The portfolio chat handler (`ChatRouter.HandlePortfolioReadAsync`) and the "sell profitable" order proposal (`ChatOrderProposalService.BuildAsync`) both sum **all** buy trades' `Quantity` without subtracting what was already sold. This means:
- Portfolio P/L is calculated on the full buy quantity (e.g., 10g even after selling 3g)
- "Sell all my profitable holdings" quotes an inflated amount
- Wallet balance (the only correct source) is displayed alongside misleading trade data

## Solution
Add a `RemainingQuantity` column to the `Trade` entity. On buy: set to `Quantity`. On sell: deduct from cheapest-purchased buy lots first (lowest `ExecutedPrice` ascending — maximizes profit). Use `RemainingQuantity` in portfolio display and sell-proposal calculations.

---

### Step 1 — Entity: `Tibr.Domain/Entities/Trade.cs`

Add property:
```csharp
public decimal RemainingQuantity { get; set; }
```

### Step 2 — Direct sells: `TradeService.cs`

**`ExecuteDirectBuyAsync`** — set `trade.RemainingQuantity = dto.Quantity`

**`ExecuteDirectSellAsync`** — after creating the sell trade, deduct from cheapest buys first:
```
remainingToSell = dto.Quantity
for each buyTrade (user + assetType, Side=Buy, RemainingQuantity>0, order by ExecutedPrice ASC):
    if remainingToSell <= 0: break
    deduction = Min(buyTrade.RemainingQuantity, remainingToSell)
    buyTrade.RemainingQuantity -= deduction
    remainingToSell -= deduction
```

### Step 3 — Strategy/auto sells: `ResolutionService.cs`

In `TryAutoExecuteAsync` (~line 172):
- **Buy strategy**: `trade.RemainingQuantity = tradeQuantity`
- **Sell strategy**: `trade.RemainingQuantity = 0` + same cheapest-first deduction as Step 2

### Step 4 — Seeding: `MassDataSeeder.cs`

Line 319-331: Add `RemainingQuantity = o.OrderType == OrderType.Buy ? qty : 0`

### Step 5 — Migration

```
dotnet ef migrations add AddRemainingQuantityToTrades
```

Add manual backfill in `Up()`:
```sql
UPDATE Trades SET RemainingQuantity = Quantity WHERE Side = 1;
UPDATE Trades SET RemainingQuantity = 0 WHERE Side = 2;
```

### Step 6 — Portfolio display: `ChatRouter.cs`

In `HandlePortfolioReadAsync` (line 204-228):
- Use `t.RemainingQuantity` for P/L: `plPerGram * t.RemainingQuantity`
- Skip trades where `t.RemainingQuantity <= 0`
- Keep wallet balance display unchanged (already correct)

### Step 7 — Sell-profitable proposal: `ChatOrderProposalService.cs`

Line 90-94: Change `.Sum(t => t.Quantity)` to `.Sum(t => t.RemainingQuantity)`

---

## Files unchanged

| File | Rationale |
|---|---|
| `TradingDtos.cs` | ChatRouter reads entity directly, not via DTO |
| `PlanService.cs` | `monthly_budget` uses buy-trade `TotalAmount` (historical) — correct; others use wallet balance — already correct |

---

## Edge cases

| Scenario | Behavior |
|---|---|
| Partial sell across multiple lots | Deducts from cheapest (lowest ExecutedPrice) first |
| Sell more than bought | Wallet balance check prevents it |
| Existing data | Backfill sets `RemainingQuantity = Quantity` for all buys |
| Strategy auto-execute (buy) | Sets `RemainingQuantity` on the new trade |
| Strategy auto-execute (sell) | Runs cheapest-first deduction like direct sell |
