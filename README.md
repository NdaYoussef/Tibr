## Branch: `investment-dev-back`

**Investment module** — New domain entities (`OrdersInvestment`, `Trade`, `StrategyCondition`, etc.), DTOs, services (buy/sell/strategy), controllers, DbSets, decimal precision config

**Payment refactor** — Replaced old `PaymobService` with provider-agnostic `IPaymentGateway` interface + `PaymobPaymentGateway` (pure HTTP, HMAC-SHA512), unified `PaymentService` orchestrator routing callbacks by `special_reference` prefix (`payment:` / `deposit:`)

**Deposit flow** — Refactored to use shared `IPaymentGateway` for intention creation, callback handling with wallet credit
