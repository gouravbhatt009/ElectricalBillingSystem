-- =====================================================================
-- Phase 1 schema: Users + BusinessSettings
-- This is provided for reference / manual setup only.
-- The recommended approach is to let EF Core generate and apply
-- migrations (see README.md -> "Database setup").
-- =====================================================================

IF DB_ID('ElectricalBillingDb') IS NULL
BEGIN
    CREATE DATABASE ElectricalBillingDb;
END
GO

USE ElectricalBillingDb;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId        INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        FullName      NVARCHAR(100)     NOT NULL,
        Username      NVARCHAR(150)     NOT NULL,
        Email         NVARCHAR(150)     NOT NULL,
        PasswordHash  NVARCHAR(MAX)     NOT NULL,
        IsActive      BIT               NOT NULL DEFAULT (1),
        CreatedAt     DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2         NULL,
        LastLoginAt   DATETIME2         NULL,

        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID('dbo.BusinessSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BusinessSettings
    (
        BusinessSettingId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        BusinessName       NVARCHAR(200)    NOT NULL,
        Description        NVARCHAR(500)    NULL,
        Address            NVARCHAR(300)    NULL,
        Mobile             NVARCHAR(20)     NULL,
        Email              NVARCHAR(150)    NULL,
        GSTIN              NVARCHAR(20)     NULL,
        InvoicePrefix      NVARCHAR(20)     NOT NULL DEFAULT ('ELB'),
        LogoPath           NVARCHAR(300)    NULL,
        SignaturePath      NVARCHAR(300)    NULL,
        GstEnabled         BIT              NOT NULL DEFAULT (0),
        DefaultGstPercent  DECIMAL(5,2)     NOT NULL DEFAULT (18.00),
        CgstPercent        DECIMAL(5,2)     NOT NULL DEFAULT (9.00),
        SgstPercent        DECIMAL(5,2)     NOT NULL DEFAULT (9.00),
        InvoiceFooter      NVARCHAR(500)    NULL,
        BankDetails        NVARCHAR(500)    NULL,
        UpiId              NVARCHAR(100)    NULL,
        CreatedAt          DATETIME2        NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt          DATETIME2        NULL
    );

    INSERT INTO dbo.BusinessSettings
        (BusinessName, Description, Address, Mobile, InvoicePrefix, GstEnabled,
         DefaultGstPercent, CgstPercent, SgstPercent, InvoiceFooter)
    VALUES
        ('LEELADHAR BHATT',
         'Power Factor, Electric Work, HT/LT Cabling, Earth Testing, Transformer Oil Testing APFC Penal & Electrical Guidance',
         'Plot No.56, Goutam Nagar-8, Khora Bisal, JAIPUR (Raj.)',
         '9413600320',
         'ELB',
         0, 18.00, 9.00, 9.00,
         'Thank you for your business.');
END
GO
