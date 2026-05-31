-- ═══════════════════════════════════════════════════════
-- Seed script: Populate local DB with test data
-- ═══════════════════════════════════════════════════════
-- Usage:
--   /opt/mssql-tools/bin/sqlcmd -S . -d TibrDb -U sa -P 'Securepass?' -i .plans/seed.sql
-- Or open this file in SSMS / Azure Data Studio and execute.
-- ═══════════════════════════════════════════════════════

-- Clean up existing test data (run if you need to re-seed)
-- DELETE FROM OrderItems;
-- DELETE FROM Orders;
-- DELETE FROM Payments;
-- DELETE FROM Products;
-- DELETE FROM Categories;
-- DELETE FROM Users;

-- ═══ Users ═══
INSERT INTO Users (FirstName, LastName, Email, Phone, Password, Status, OtpVerified, KycStatus, CreatedAt, IsDeleted)
VALUES
    ('Ahmed', 'Ali',     'ahmed@example.com',   '01000000001', 'hashed_pwd_1', 'Active', 1, 'Verified', GETUTCDATE(), 0),
    ('Sara',  'Hassan',  'sara@example.com',    '01000000002', 'hashed_pwd_2', 'Active', 1, 'Pending',  GETUTCDATE(), 0);

-- ═══ Categories ═══
INSERT INTO Categories (Name, CreatedAt, IsDeleted)
VALUES
    ('Gold Rings',       GETUTCDATE(), 0),
    ('Gold Necklaces',   GETUTCDATE(), 0),
    ('Gold Bracelets',   GETUTCDATE(), 0),
    ('Gold Earrings',    GETUTCDATE(), 0);

-- ═══ Products ═══
INSERT INTO Products (CategoryId, Name, MetalType, Purity, Weight, BuyPrice, SellPrice, Status, CreatedAt, IsDeleted)
VALUES
    (1, '18K Gold Band Ring',        'Gold', 18.0000,  5.000, 250.00,  320.00,  'Available', GETUTCDATE(), 0),
    (1, '21K Diamond Engagement',    'Gold', 21.0000,  8.500, 650.00,  780.00,  'Available', GETUTCDATE(), 0),
    (2, '24K Gold Chain Necklace',   'Gold', 24.0000, 15.000, 1200.00, 1450.00, 'Available', GETUTCDATE(), 0),
    (3, '18K Gold Tennis Bracelet',  'Gold', 18.0000, 10.000, 500.00,  620.00,  'Available', GETUTCDATE(), 0),
    (4, '21K Gold Hoop Earrings',    'Gold', 21.0000,  6.000, 400.00,  510.00,  'Available', GETUTCDATE(), 0);

-- ═══ Orders ═══
INSERT INTO Orders (UserId, OrderNumber, TotalAmount, PaymentStatus, OrderStatus, CreatedAt, IsDeleted)
VALUES
    (1, 'ORD-a1b2c3d4-0001', 1260.00, 'Paid',   'Shipped',   DATEADD(DAY, -5, GETUTCDATE()), 0),
    (1, 'ORD-e5f6g7h8-0002', 780.00,  'Paid',   'Delivered', DATEADD(DAY, -2, GETUTCDATE()), 0),
    (2, 'ORD-i9j0k1l2-0003', 510.00,  'Unpaid', 'Pending',   GETUTCDATE(), 0);

-- ═══ OrderItems ═══
INSERT INTO OrderItems (OrderId, ProductId, Quantity, Price, CreatedAt, IsDeleted)
VALUES
    (1, 1, 2, 320.00, GETUTCDATE(), 0),  -- 2 × Band Ring @ 320 = 640
    (1, 4, 1, 620.00, GETUTCDATE(), 0),  -- 1 × Tennis Bracelet @ 620 = 620  → Order Total = 1260
    (2, 2, 1, 780.00, GETUTCDATE(), 0),  -- 1 × Diamond Engagement @ 780 = 780
    (3, 5, 1, 510.00, GETUTCDATE(), 0);  -- 1 × Hoop Earrings @ 510 = 510

-- ═══ Verify ═══
SELECT 'Users' AS TableName, COUNT(*) AS Rows FROM Users
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Products', COUNT(*) FROM Products
UNION ALL SELECT 'Orders', COUNT(*) FROM Orders
UNION ALL SELECT 'OrderItems', COUNT(*) FROM OrderItems;

PRINT '';
PRINT '✅ Seed complete! Test data inserted.';
