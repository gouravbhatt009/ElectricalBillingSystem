-- =====================================================================
-- Phase 2 schema additions: Customers + Services
-- Run this after Script_Phase1_InitialCreate.sql if you're using manual
-- SQL setup instead of EF Core migrations.
-- =====================================================================

USE ElectricalBillingDb;
GO

IF OBJECT_ID('dbo.Customers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        CustomerName     NVARCHAR(150)     NOT NULL,
        CompanyName      NVARCHAR(150)     NULL,
        Address          NVARCHAR(300)     NULL,
        City             NVARCHAR(100)     NULL,
        State            NVARCHAR(100)     NULL,
        Pincode          NVARCHAR(10)      NULL,
        Mobile           NVARCHAR(20)      NOT NULL,
        AlternateMobile  NVARCHAR(20)      NULL,
        Email            NVARCHAR(150)     NULL,
        GSTIN            NVARCHAR(20)      NULL,
        Notes            NVARCHAR(1000)    NULL,
        IsActive         BIT               NOT NULL DEFAULT (1),
        CreatedAt        DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt        DATETIME2         NULL
    );

    CREATE INDEX IX_Customers_Mobile ON dbo.Customers (Mobile);
    CREATE INDEX IX_Customers_CustomerName ON dbo.Customers (CustomerName);
END
GO

IF OBJECT_ID('dbo.Services', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Services
    (
        ServiceId     INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        ServiceName   NVARCHAR(150)     NOT NULL,
        Description   NVARCHAR(500)     NULL,
        DefaultRate   DECIMAL(10,2)     NULL,
        IsActive      BIT               NOT NULL DEFAULT (1),
        CreatedAt     DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_Services_ServiceName ON dbo.Services (ServiceName);

    INSERT INTO dbo.Services (ServiceName, IsActive, CreatedAt) VALUES
        ('Power Factor Maintenance', 1, SYSUTCDATETIME()),
        ('Electric Work', 1, SYSUTCDATETIME()),
        ('HT/LT Cabling', 1, SYSUTCDATETIME()),
        ('Earth Testing', 1, SYSUTCDATETIME()),
        ('Transformer Oil Testing', 1, SYSUTCDATETIME()),
        ('APFC Panel', 1, SYSUTCDATETIME()),
        ('Electrical Guidance', 1, SYSUTCDATETIME());
END
GO
