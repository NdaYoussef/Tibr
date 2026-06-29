-- ══════════════════════════════════════════════════════
-- Tibr Database — Rich Seed Script
-- ══════════════════════════════════════════════════════
-- Usage:
--   1. Ensure migrations are applied (dotnet ef database update)
--   2. Run: sqlcmd -S localhost -d TibrDb -U sa -P 'Securepass?' -i seed.sql
--   3. Or paste this into SSMS / Azure Data Studio
-- ══════════════════════════════════════════════════════

USE [TibrDb];
GO

-- ── Cleanup (repeatable run) ──────────────────────────
DELETE FROM WalletTransactions;
DELETE FROM Reservations;
DELETE FROM Transactions;
DELETE FROM Trades;
DELETE FROM Alerts;
DELETE FROM OrderConditions;
DELETE FROM OrdersInvestments;
DELETE FROM CartItems;
DELETE FROM Carts;
DELETE FROM Favorites;
DELETE FROM Notifications;
DELETE FROM Tickets;
DELETE FROM Supports;
DELETE FROM DeliveryRequests;
DELETE FROM Addresses;
DELETE FROM Payments;
DELETE FROM OrderItems;
DELETE FROM Orders;
DELETE FROM KYCDocuments;
DELETE FROM AuditLogs;
DELETE FROM Deposits;
DELETE FROM Wallets;
DELETE FROM AssetPrices;
DELETE FROM Products;
DELETE FROM Categories;
DELETE FROM Admins;
DELETE FROM Users;
GO

-- ── Reset identity seeds (idempotent re-run support) ────
DBCC CHECKIDENT ('WalletTransactions', RESEED, 0);
DBCC CHECKIDENT ('Reservations',       RESEED, 0);
DBCC CHECKIDENT ('Transactions',       RESEED, 0);
DBCC CHECKIDENT ('Trades',             RESEED, 0);
DBCC CHECKIDENT ('Alerts',             RESEED, 0);
DBCC CHECKIDENT ('OrderConditions',    RESEED, 0);
DBCC CHECKIDENT ('OrdersInvestments',  RESEED, 0);
DBCC CHECKIDENT ('CartItems',          RESEED, 0);
DBCC CHECKIDENT ('Carts',              RESEED, 0);
DBCC CHECKIDENT ('Favorites',          RESEED, 0);
DBCC CHECKIDENT ('Notifications',      RESEED, 0);
DBCC CHECKIDENT ('Tickets',            RESEED, 0);
DBCC CHECKIDENT ('Supports',           RESEED, 0);
DBCC CHECKIDENT ('DeliveryRequests',   RESEED, 0);
DBCC CHECKIDENT ('Addresses',          RESEED, 0);
DBCC CHECKIDENT ('Payments',           RESEED, 0);
DBCC CHECKIDENT ('OrderItems',         RESEED, 0);
DBCC CHECKIDENT ('Orders',             RESEED, 0);
DBCC CHECKIDENT ('KYCDocuments',       RESEED, 0);
DBCC CHECKIDENT ('AuditLogs',          RESEED, 0);
DBCC CHECKIDENT ('Deposits',           RESEED, 0);
DBCC CHECKIDENT ('Wallets',            RESEED, 0);
DBCC CHECKIDENT ('AssetPrices',        RESEED, 0);
DBCC CHECKIDENT ('Products',           RESEED, 0);
DBCC CHECKIDENT ('Categories',         RESEED, 0);
DBCC CHECKIDENT ('Admins',             RESEED, 0);
DBCC CHECKIDENT ('Users',              RESEED, 0);
GO

-- ══════════════════════════════════════════════════════
-- 1. ADMINS
-- ══════════════════════════════════════════════════════
INSERT INTO Admins
    (Name, Email, Status, CreatedAt, IsDeleted)
VALUES
    ('Admin Tibr', 'admin@tibr.com', 'Active', GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 2. USERS
-- ══════════════════════════════════════════════════════
-- Password for all users: "Test@123" (BCrypt hash)
DECLARE @HashedPwd NVARCHAR(200) = '$2a$11$fDKF25CzMbPmms6ZQTj7XedMVfHdqK9dGhH2prtPLXJu8XaZIvbeu';

INSERT INTO Users
    (FirstName, LastName, Email, Phone, Password, Status, OtpVerified, KycStatus, OtpCode, OtpExpiry, CreatedAt, IsDeleted)
VALUES
    ('Ahmed', 'Ali', 'ahmed@tibr.com', '01010000001', @HashedPwd, 'Active', 1, 'Verified', '123456', DATEADD(MINUTE, 30, GETUTCDATE()), GETUTCDATE(), 0),
    ('Sara', 'Hassan', 'sara@tibr.com', '01010000002', @HashedPwd, 'Active', 1, 'Verified', '123456', DATEADD(MINUTE, 30, GETUTCDATE()), GETUTCDATE(), 0),
    ('Mohamed', 'Ibrahim', 'mohamed@tibr.com', '01010000003', @HashedPwd, 'Active', 1, 'Pending', '123456', DATEADD(MINUTE, 30, GETUTCDATE()), GETUTCDATE(), 0),
    ('Nour', 'Khaled', 'nour@tibr.com', '01010000004', @HashedPwd, 'Active', 0, 'NotSubmitted', NULL, NULL, GETUTCDATE(), 0),
    ('Eslam', 'Abd-Elsatar', 'eslamlegend5@gmail.com', '01010000005', @HashedPwd, 'Active', 1, 'Verified', '123456', DATEADD(MINUTE, 30, GETUTCDATE()), GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 3. CATEGORIES
-- ══════════════════════════════════════════════════════
INSERT INTO Categories
    (Name, CreatedAt, IsDeleted)
VALUES
    ('Gold Bars & Bullion', GETUTCDATE(), 0),
    ('Gold Coins', GETUTCDATE(), 0),
    ('Silver Bars', GETUTCDATE(), 0),
    ('Silver Coins', GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 4. PRODUCTS
-- ══════════════════════════════════════════════════════
INSERT INTO Products
    (CategoryId, Name, MetalType, Purity, Weight, BuyPrice, SellPrice, Status, Stock, ImageUrl, CreatedAt, UpdatedAt, IsDeleted)
VALUES
    -- Gold Bars (Category 1)
    (1, '1g Gold Bar - 24K', 'Gold', 24.0000, 1.000, 8500.00, 8450.00, 'Active', 50, NULL, GETUTCDATE(), NULL, 0),
    (1, '5g Gold Bar - 24K', 'Gold', 24.0000, 5.000, 42000.00, 41750.00, 'Active', 30, NULL, GETUTCDATE(), NULL, 0),
    (1, '10g Gold Bar - 24K', 'Gold', 24.0000, 10.000, 83000.00, 82500.00, 'Active', 20, NULL, GETUTCDATE(), NULL, 0),
    (1, '50g Gold Bar - 24K', 'Gold', 24.0000, 50.000, 410000.00, 408000.00, 'Active', 5, NULL, GETUTCDATE(), NULL, 0),
    (1, '100g Gold Bar - 24K', 'Gold', 24.0000, 100.000, 815000.00, 810000.00, 'Active', 2, NULL, GETUTCDATE(), NULL, 0),

    -- Gold Coins (Category 2)
    (2, '1oz Gold Eagle Coin', 'Gold', 22.0000, 31.103, 95000.00, 94000.00, 'Active', 15, NULL, GETUTCDATE(), NULL, 0),
    (2, '1/2oz Gold Eagle Coin', 'Gold', 22.0000, 15.552, 48000.00, 47500.00, 'Active', 25, NULL, GETUTCDATE(), NULL, 0),
    (2, '1/4oz Gold Eagle Coin', 'Gold', 22.0000, 7.776, 24500.00, 24200.00, 'Active', 40, NULL, GETUTCDATE(), NULL, 0),
    (2, '1oz Gold Maple Leaf Coin', 'Gold', 24.0000, 31.103, 96000.00, 95000.00, 'Active', 12, NULL, GETUTCDATE(), NULL, 0),

    -- Silver Bars (Category 3)
    (3, '10g Silver Bar - 999', 'Silver', 99.9000, 10.000, 135.00, 130.00, 'Active', 200, NULL, GETUTCDATE(), NULL, 0),
    (3, '50g Silver Bar - 999', 'Silver', 99.9000, 50.000, 650.00, 635.00, 'Active', 100, NULL, GETUTCDATE(), NULL, 0),
    (3, '100g Silver Bar - 999', 'Silver', 99.9000, 100.000, 1280.00, 1260.00, 'Active', 75, NULL, GETUTCDATE(), NULL, 0),
    (3, '250g Silver Bar - 999', 'Silver', 99.9000, 250.000, 3150.00, 3100.00, 'Active', 30, NULL, GETUTCDATE(), NULL, 0),
    (3, '1kg Silver Bar - 999', 'Silver', 99.9000, 1000.000, 12500.00, 12300.00, 'Active', 10, NULL, GETUTCDATE(), NULL, 0),

    -- Silver Coins (Category 4)
    (4, '1oz Silver Eagle Coin', 'Silver', 99.9000, 31.103, 420.00, 410.00, 'Active', 100, NULL, GETUTCDATE(), NULL, 0),
    (4, '1/2oz Silver Eagle Coin', 'Silver', 99.9000, 15.552, 215.00, 208.00, 'Active', 150, NULL, GETUTCDATE(), NULL, 0),
    (4, '1oz Silver Maple Leaf Coin', 'Silver', 99.9900, 31.103, 425.00, 415.00, 'Active', 80, NULL, GETUTCDATE(), NULL, 0);
GO

-- ══════════════════════════════════════════════════════
-- 5. ASSET PRICES (Gold & Silver spot)
-- ══════════════════════════════════════════════════════
-- AssetType: Gold=1, Silver=2
INSERT INTO AssetPrices
    (AssetType, BuyPrice, SellPrice, Source, CreatedAt, IsDeleted)
VALUES
    (1, 8500.0000, 8450.0000, 'LiveSpot-API', GETUTCDATE(), 0),
    -- Gold per gram
    (2, 135.0000, 130.0000, 'LiveSpot-API', GETUTCDATE(), 0); -- Silver per gram
GO

-- ══════════════════════════════════════════════════════
-- 6. WALLETS
-- ══════════════════════════════════════════════════════
-- WalletType: Cash=1, Gold=2, Silver=3
-- User 1 — Ahmed
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (1, 1, 500000.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (1, 2, 50.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (1, 3, 200.0000, 0.0000, GETUTCDATE(), 0);

-- User 2 — Sara
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (2, 1, 750000.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (2, 2, 100.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (2, 3, 500.0000, 0.0000, GETUTCDATE(), 0);

-- User 3 — Mohamed
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (3, 1, 100000.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (3, 2, 0.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (3, 3, 50.0000, 0.0000, GETUTCDATE(), 0);

-- User 4 — Nour
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (4, 1, 25000.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (4, 2, 0.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (4, 3, 10.0000, 0.0000, GETUTCDATE(), 0);

-- User 5 — Eslam
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (5, 1, 1000000.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (5, 2, 100.0000, 0.0000, GETUTCDATE(), 0);
INSERT INTO Wallets
    (UserId, WalletType, Balance, ReservedBalance, CreatedAt, IsDeleted)
VALUES
    (5, 3, 500.0000, 0.0000, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 7. ADDRESSES
-- ══════════════════════════════════════════════════════
INSERT INTO Addresses
    (UserId, City, Area, Street, Building, PostalCode, IsDefault, CreatedAt, IsDeleted)
VALUES
    (1, 'Cairo', 'Maadi', 'Corniche El Nil', '15A', '11728', 1, GETUTCDATE(), 0),
    (1, 'Cairo', 'New Cairo', 'Fifth Settlement', '42', '11835', 0, GETUTCDATE(), 0),
    (2, 'Alexandria', 'Smouha', 'El Gaish Rd', '8', '21615', 1, GETUTCDATE(), 0),
    (3, 'Giza', 'Sheikh Zayed', 'Al Nahda St', '22', '12588', 1, GETUTCDATE(), 0),
    (4, 'Cairo', 'Nasr City', 'Abbas El Akkad', '7', '11765', 1, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 8. ORDERS
-- ══════════════════════════════════════════════════════
INSERT INTO Orders
    (UserId, OrderNumber, TotalAmount, PaymentStatus, OrderStatus, CreatedAt, UpdatedAt, IsDeleted)
VALUES
    (1, 'ORD-AHMED-000001', 8500.00, 'Paid', 'Processing', DATEADD(DAY, -3, GETUTCDATE()), NULL, 0),
    (1, 'ORD-AHMED-000002', 42000.00, 'Paid', 'Shipped', DATEADD(DAY, -2, GETUTCDATE()), NULL, 0),
    (2, 'ORD-SARA-000001', 1350.00, 'Paid', 'Delivered', DATEADD(DAY, -7, GETUTCDATE()), NULL, 0),
    (2, 'ORD-SARA-000002', 96000.00, 'Unpaid', 'Pending', GETUTCDATE(), NULL, 0),
    (3, 'ORD-MOHAMED-00001', 420.00, 'Unpaid', 'Cancelled', DATEADD(DAY, -1, GETUTCDATE()), GETUTCDATE(), 1),
    (4, 'ORD-NOUR-000001', 4250.00, 'Paid', 'Shipped', DATEADD(DAY, -5, GETUTCDATE()), NULL, 0);
GO

-- ══════════════════════════════════════════════════════
-- 9. ORDER ITEMS
-- ══════════════════════════════════════════════════════
INSERT INTO OrderItems
    (OrderId, ProductId, Quantity, Price, CreatedAt, IsDeleted)
VALUES
    -- Order 1: Ahmed - 1g Gold Bar
    (1, 1, 1, 8450.00, GETUTCDATE(), 0),

    -- Order 2: Ahmed - 5g Gold Bar
    (2, 2, 1, 41750.00, GETUTCDATE(), 0),

    -- Order 3: Sara - 10x 10g Silver Bar
    (3, 11, 10, 135.00, GETUTCDATE(), 0),

    -- Order 4: Sara - 1oz Gold Maple (unpaid)
    (4, 9, 1, 95000.00, GETUTCDATE(), 0),

    -- Order 5: Mohamed - 1oz Silver Eagle (cancelled)
    (5, 15, 1, 420.00, GETUTCDATE(), 0),

    -- Order 6: Nour - 5g Gold Bar / 1/2oz Gold Eagle
    (6, 2, 1, 41750.00, GETUTCDATE(), 0),
    (6, 7, 1, 47500.00, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 10. PAYMENTS
-- ══════════════════════════════════════════════════════
INSERT INTO Payments
    (OrderId, UserId, PaymentMethod, Amount, Status, PaidAt, CreatedAt, IsDeleted)
VALUES
    (1, 1, 'Paymob', 8500.00, 'Completed', DATEADD(DAY, -3, GETUTCDATE()), GETUTCDATE(), 0),
    (2, 1, 'Paymob', 42000.00, 'Completed', DATEADD(DAY, -2, GETUTCDATE()), GETUTCDATE(), 0),
    (3, 2, 'Paymob', 1350.00, 'Completed', DATEADD(DAY, -7, GETUTCDATE()), GETUTCDATE(), 0),
    (6, 4, 'Paymob', 4250.00, 'Completed', DATEADD(DAY, -5, GETUTCDATE()), GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 11. CARTS (active carts with items)
-- ══════════════════════════════════════════════════════
DECLARE @Cart1Id BIGINT;
INSERT INTO Carts
    (UserId, CreatedAt, IsDeleted)
VALUES
    (3, GETUTCDATE(), 0);
SET @Cart1Id = SCOPE_IDENTITY();
INSERT INTO CartItems
    (CartId, ProductId, Quantity, UnitPrice, CreatedAt, IsDeleted)
VALUES
    (@Cart1Id, 3, 1, 83000.00, GETUTCDATE(), 0),
    -- 10g Gold Bar
    (@Cart1Id, 16, 5, 420.00, GETUTCDATE(), 0);
-- 5x 1/2oz Silver Eagle

DECLARE @Cart2Id BIGINT;
INSERT INTO Carts
    (UserId, CreatedAt, IsDeleted)
VALUES
    (4, GETUTCDATE(), 0);
SET @Cart2Id = SCOPE_IDENTITY();
INSERT INTO CartItems
    (CartId, ProductId, Quantity, UnitPrice, CreatedAt, IsDeleted)
VALUES
    (@Cart2Id, 15, 2, 420.00, GETUTCDATE(), 0);  -- 2x 1oz Silver Eagle
GO

-- ══════════════════════════════════════════════════════
-- 12. FAVORITES
-- ══════════════════════════════════════════════════════
INSERT INTO Favorites
    (UserId, ProductId, CreatedAt, IsDeleted)
VALUES
    (1, 1, GETUTCDATE(), 0),
    (1, 6, GETUTCDATE(), 0),
    (2, 9, GETUTCDATE(), 0),
    (3, 3, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 13. NOTIFICATIONS
-- ══════════════════════════════════════════════════════
INSERT INTO Notifications
    (UserId, Title, Message, IsRead, CreatedAt, IsDeleted)
VALUES
    (1, 'Order Shipped', 'Your order ORD-AHMED-000002 has been shipped.', 0, DATEADD(HOUR, -12, GETUTCDATE()), 0),
    (1, 'Welcome', 'Welcome to Tibr! Start investing in precious metals.', 1, GETUTCDATE(), 0),
    (2, 'Order Delivered', 'Your order ORD-SARA-000001 has been delivered.', 0, DATEADD(HOUR, -24, GETUTCDATE()), 0),
    (3, 'KYC Pending', 'Your identity verification is pending review.', 0, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 14. SUPPORT TICKETS
-- ══════════════════════════════════════════════════════
-- SupportStatus: Open=1, Pending=2, Resolved=3, Closed=4
INSERT INTO Supports
    (UserId, Subject, Status, CreatedAt, IsDeleted)
VALUES
    (1, 'Delivery delay on order ORD-AHMED-000002', 2, DATEADD(DAY, -1, GETUTCDATE()), 0),
    (2, 'Payment not reflected in wallet', 1, GETUTCDATE(), 0);

-- Tickets (replies)
-- Ticket.AdminId is required but we use the only admin (Id=1)
INSERT INTO Tickets
    (AdminId, SupportId, Message, CreatedAt, IsDeleted)
VALUES
    (1, 1, 'We are looking into this. Your package is with the courier.', DATEADD(HOUR, -12, GETUTCDATE()), 0);
GO

-- ══════════════════════════════════════════════════════
-- 15. DEPOSITS
-- ══════════════════════════════════════════════════════
-- DepositStatus: Pending=1, Completed=2, Failed=3
-- PaymentMethod: Paymob=1, Visa=2
INSERT INTO Deposits
    (UserId, Amount, Status, PaymentMethod, TransactionRef, CreatedAt, IsDeleted)
VALUES
    (1, 50000.00, 2, 1, 'DEP-TEST-001', DATEADD(DAY, -10, GETUTCDATE()), 0),
    (1, 25000.00, 2, 1, 'DEP-TEST-002', DATEADD(DAY,  -5, GETUTCDATE()), 0),
    (2, 100000.00, 2, 2, 'DEP-TEST-003', DATEADD(DAY, -14, GETUTCDATE()), 0),
    (3, 10000.00, 2, 1, 'DEP-TEST-004', DATEADD(DAY,  -3, GETUTCDATE()), 0),
    (4, 25000.00, 2, 1, 'DEP-TEST-005', DATEADD(DAY,  -7, GETUTCDATE()), 0);
GO

-- ══════════════════════════════════════════════════════
-- 16. INVESTMENT ORDERS (Strategy orders)
-- ══════════════════════════════════════════════════════
-- OrderStatus: Pending=1, Triggered=2, Executed=3, Cancelled=4, Failed=5, Expired=6
-- AssetType: Gold=1, Silver=2
-- OrderType: Buy=1, Sell=2
-- ExecutionMode: Direct=1, Strategy=2
-- ExecutionType: AlertOnly=1, AutoExecute=2, AlertAndExecute=3
INSERT INTO OrdersInvestments
    (UserId, AssetType, OrderType, ExecutionMode, Quantity, RequestedPrice, CurrentPrice, Status, ExecutionType, ExpiryDate, CreatedAt, IsDeleted)
VALUES
    -- Ahmed: Buy Gold @ 8400 (pending, auto-execute when price dips)
    (1, 1, 1, 2, 10.0000, 8400.0000, 8500.0000, 1, 2, DATEADD(MONTH, 1, GETUTCDATE()), GETUTCDATE(), 0),
    -- Sara: Sell Silver @ 140 (pending, alert-only)
    (2, 2, 2, 2, 100.0000, 140.0000, 135.0000, 1, 1, DATEADD(MONTH, 2, GETUTCDATE()), GETUTCDATE(), 0),
    -- Ahmed: Buy Silver @ 128 (executed)
    (1, 2, 1, 1, 50.0000, 128.0000, 135.0000, 3, 3, NULL, DATEADD(DAY, -4, GETUTCDATE()), 0),

    -- ═══ Wallet-holding trades (explain seeded wallet balances) ═══
    -- ExecutionMode: Direct=1, ExecutionType: AutoExecute=2, Status: Executed=3
    -- Ahmed: Buy Gold 50g @ 8000 (to explain 50g gold wallet)
    (1, 1, 1, 1, 50.0000, 8000.0000, 8500.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Ahmed: Buy Silver 150g @ 120 (remaining 150g beyond existing 50g trade → 200g total)
    (1, 2, 1, 1, 150.0000, 120.0000, 135.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Sara: Buy Gold 100g @ 8000
    (2, 1, 1, 1, 100.0000, 8000.0000, 8500.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Sara: Buy Silver 500g @ 120
    (2, 2, 1, 1, 500.0000, 120.0000, 135.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Mohamed: Buy Silver 50g @ 120
    (3, 2, 1, 1, 50.0000, 120.0000, 135.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Nour: Buy Silver 10g @ 120
    (4, 2, 1, 1, 10.0000, 120.0000, 135.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Eslam: Buy Gold 100g @ 8000
    (5, 1, 1, 1, 100.0000, 8000.0000, 8500.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Eslam: Buy Silver 500g @ 120
    (5, 2, 1, 1, 500.0000, 120.0000, 135.0000, 3, 2, NULL, DATEADD(DAY, -10, GETUTCDATE()), 0);
GO

-- ══════════════════════════════════════════════════════
-- 17. ORDER CONDITIONS (for strategy orders)
-- ══════════════════════════════════════════════════════
-- ConditionType: PriceTarget=1
-- ConditionOperator: GreaterThan=1, GreaterThanOrEqual=2, LessThan=3, LessThanOrEqual=4, Equal=5
INSERT INTO OrderConditions
    (OrderId, ConditionType, Operator, TargetValue, CreatedAt, IsDeleted)
VALUES
    (1, 1, 4, 8400.0000, GETUTCDATE(), 0),
    -- Price <= 8400 → trigger buy
    (2, 1, 1, 140.0000, GETUTCDATE(), 0);      -- Price >= 140 → alert
GO

-- ══════════════════════════════════════════════════════
-- 18. TRADES (for executed investment orders)
-- ══════════════════════════════════════════════════════
-- TradeSide: Buy=1, Sell=2
INSERT INTO Trades
    (OrderId, UserId, AssetType, Side, Quantity, ExecutedPrice, TotalAmount, ExecutedAt, CreatedAt, IsDeleted)
VALUES
    -- Existing: Ahmed Buy Silver 50g @ 128 (via strategy order)
    (3, 1, 2, 1, 50.0000, 128.0000, 6400.00, DATEADD(DAY, -4, GETUTCDATE()), GETUTCDATE(), 0),

    -- ═══ Wallet-holding trades ═══
    -- Ahmed: Gold 50g @ 8000
    (4, 1, 1, 1, 50.0000, 8000.0000, 400000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Ahmed: Silver 150g @ 120
    (5, 1, 2, 1, 150.0000, 120.0000, 18000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Sara: Gold 100g @ 8000
    (6, 2, 1, 1, 100.0000, 8000.0000, 800000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Sara: Silver 500g @ 120
    (7, 2, 2, 1, 500.0000, 120.0000, 60000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Mohamed: Silver 50g @ 120
    (8, 3, 2, 1, 50.0000, 120.0000, 6000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Nour: Silver 10g @ 120
    (9, 4, 2, 1, 10.0000, 120.0000, 1200.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Eslam: Gold 100g @ 8000
    (10, 5, 1, 1, 100.0000, 8000.0000, 800000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0),
    -- Eslam: Silver 500g @ 120
    (11, 5, 2, 1, 500.0000, 120.0000, 60000.00, DATEADD(DAY, -10, GETUTCDATE()), GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- 19. TRANSACTIONS (for completed trades)
-- ══════════════════════════════════════════════════════
-- TransactionType: Buy=1, Sell=2
-- TransactionStatusEnum: Success=1, Failed=2
INSERT INTO Transactions
    (UserId, TradeId, TransactionType, Amount, Status, CreatedAt, IsDeleted)
VALUES
    -- Existing: Ahmed Silver 50g @ 128
    (1, 1, 1, 6400.00, 1, DATEADD(DAY, -4, GETUTCDATE()), 0),

    -- ═══ Wallet-holding trades ═══
    (1, 2, 1, 400000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (1, 3, 1, 18000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (2, 4, 1, 800000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (2, 5, 1, 60000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (3, 6, 1, 6000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (4, 7, 1, 1200.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (5, 8, 1, 800000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (5, 9, 1, 60000.00, 1, DATEADD(DAY, -10, GETUTCDATE()), 0);
GO

-- ══════════════════════════════════════════════════════
-- 20. WALLET TRANSACTIONS (audit trail for wallet movements)
-- ══════════════════════════════════════════════════════
-- WalletTransactionType: Credit=1, Debit=2, Reserve=3, Release=4
-- ReferenceType: Trade=2
-- Wallet IDs: User1(Cash=1,Gold=2,Silver=3); User2(Cash=4,Gold=5,Silver=6)
--            User3(Cash=7,Gold=8,Silver=9); User4(Cash=10,Gold=11,Silver=12)
--            User5(Cash=13,Gold=14,Silver=15)
INSERT INTO WalletTransactions
    (WalletId, Type, Amount, ReferenceType, ReferenceId, CreatedAt, IsDeleted)
VALUES
    -- Ahmed: Gold 50g @ 8000 = 400,000 EGP
    (1, 2, 400000.00, 2, 2, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Cash debit
    (2, 1, 50.0000, 2, 2, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Gold credit
    -- Ahmed: Silver 150g @ 120 = 18,000 EGP
    (1, 2, 18000.00, 2, 3, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Cash debit
    (3, 1, 150.0000, 2, 3, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Silver credit
    -- Sara: Gold 100g @ 8000 = 800,000 EGP
    (4, 2, 800000.00, 2, 4, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (5, 1, 100.0000, 2, 4, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Sara: Silver 500g @ 120 = 60,000 EGP
    (4, 2, 60000.00, 2, 5, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (6, 1, 500.0000, 2, 5, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Mohamed: Silver 50g @ 120 = 6,000 EGP
    (7, 2, 6000.00, 2, 6, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (9, 1, 50.0000, 2, 6, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Nour: Silver 10g @ 120 = 1,200 EGP
    (10, 2, 1200.00, 2, 7, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (12, 1, 10.0000, 2, 7, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Eslam: Gold 100g @ 8000 = 800,000 EGP
    (13, 2, 800000.00, 2, 8, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (14, 1, 100.0000, 2, 8, DATEADD(DAY, -10, GETUTCDATE()), 0),
    -- Eslam: Silver 500g @ 120 = 60,000 EGP
    (13, 2, 60000.00, 2, 9, DATEADD(DAY, -10, GETUTCDATE()), 0),
    (15, 1, 500.0000, 2, 9, DATEADD(DAY, -10, GETUTCDATE()), 0);
GO

-- ══════════════════════════════════════════════════════
-- 21. RESERVATIONS (for active investment orders)
-- ══════════════════════════════════════════════════════
-- ReservationStatus: Active=1, Released=2, Consumed=3
-- Buy 10g Gold @ 8400 → needs 84,000 EGP reserved from cash wallet
-- WalletId for Ahmed's Cash = 1 (from Wallet inserts above)
INSERT INTO Reservations
    (UserId, WalletId, OrderId, Amount, Status, CreatedAt, IsDeleted)
VALUES
    (1, 1, 1, 84000.0000, 1, GETUTCDATE(), 0);
GO

-- ══════════════════════════════════════════════════════
-- ✅ VERIFICATION
-- ══════════════════════════════════════════════════════
    SELECT 'Admins'              AS [Table], COUNT(*) AS [Rows]
    FROM Admins
UNION ALL
    SELECT 'Users', COUNT(*)
    FROM Users
UNION ALL
    SELECT 'Categories', COUNT(*)
    FROM Categories
UNION ALL
    SELECT 'Products', COUNT(*)
    FROM Products
UNION ALL
    SELECT 'AssetPrices', COUNT(*)
    FROM AssetPrices
UNION ALL
    SELECT 'Wallets', COUNT(*)
    FROM Wallets
UNION ALL
    SELECT 'Addresses', COUNT(*)
    FROM Addresses
UNION ALL
    SELECT 'Orders', COUNT(*)
    FROM Orders
UNION ALL
    SELECT 'OrderItems', COUNT(*)
    FROM OrderItems
UNION ALL
    SELECT 'Payments', COUNT(*)
    FROM Payments
UNION ALL
    SELECT 'Carts', COUNT(*)
    FROM Carts
UNION ALL
    SELECT 'CartItems', COUNT(*)
    FROM CartItems
UNION ALL
    SELECT 'Favorites', COUNT(*)
    FROM Favorites
UNION ALL
    SELECT 'Notifications', COUNT(*)
    FROM Notifications
UNION ALL
    SELECT 'Supports', COUNT(*)
    FROM Supports
UNION ALL
    SELECT 'Tickets', COUNT(*)
    FROM Tickets
UNION ALL
    SELECT 'Deposits', COUNT(*)
    FROM Deposits
UNION ALL
    SELECT 'OrdersInvestments', COUNT(*)
    FROM OrdersInvestments
UNION ALL
    SELECT 'OrderConditions', COUNT(*)
    FROM OrderConditions
UNION ALL
    SELECT 'Trades', COUNT(*)
    FROM Trades
UNION ALL
    SELECT 'Transactions', COUNT(*)
    FROM Transactions
UNION ALL
    SELECT 'WalletTransactions', COUNT(*)
    FROM WalletTransactions
UNION ALL
    SELECT 'Reservations', COUNT(*)
    FROM Reservations
ORDER BY [Table];
GO

PRINT '';
-- ============================================
-- PriceSnapshots — historical data for analytics AI
-- AssetType: 1=Gold, 2=Silver
-- ============================================
INSERT INTO PriceSnapshots (AssetType, Price, SnapshotDate, CreatedAt, IsDeleted)
VALUES
    (1, 6480.00, DATEADD(DAY, -29, GETUTCDATE()), GETUTCDATE(), 0),
    (1, 6520.00, DATEADD(DAY, -22, GETUTCDATE()), GETUTCDATE(), 0),
    (1, 6600.00, DATEADD(DAY, -15, GETUTCDATE()), GETUTCDATE(), 0),
    (1, 6580.00, DATEADD(DAY, -8,  GETUTCDATE()), GETUTCDATE(), 0),
    (1, 6502.45, DATEADD(DAY, -1,  GETUTCDATE()), GETUTCDATE(), 0),
    (2, 132.00,  DATEADD(DAY, -29, GETUTCDATE()), GETUTCDATE(), 0),
    (2, 135.00,  DATEADD(DAY, -22, GETUTCDATE()), GETUTCDATE(), 0),
    (2, 138.00,  DATEADD(DAY, -15, GETUTCDATE()), GETUTCDATE(), 0),
    (2, 136.00,  DATEADD(DAY, -8,  GETUTCDATE()), GETUTCDATE(), 0),
    (2, 133.50,  DATEADD(DAY, -1,  GETUTCDATE()), GETUTCDATE(), 0);
GO

PRINT '╔══════════════════════════════════════════╗';
PRINT '║  ✅ Tibr seed complete!                  ║';
PRINT '║  All test data inserted successfully.    ║';
PRINT '╚══════════════════════════════════════════╝';
GO



