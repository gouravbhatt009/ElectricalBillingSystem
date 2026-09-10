-- =====================================================================
-- Phase 3 schema additions: Invoices + InvoiceItems
-- Run this after Script_Phase1_InitialCreate.sql and
-- Script_Phase2_CustomersAndServices.sql if you're using manual SQL
-- setup instead of EF Core migrations.
-- =====================================================================

USE ElectricalBillingDb;
GO

IF OBJECT_ID('dbo.Invoices', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Invoices
    (
        InvoiceId      INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceNumber  NVARCHAR(30)      NOT NULL,
        CustomerId     INT               NOT NULL,
        InvoiceDate    DATETIME2         NOT NULL,
        GstEnabled     BIT               NOT NULL DEFAULT (0),
        CgstPercent    DECIMAL(5,2)      NOT NULL DEFAULT (0),
        SgstPercent    DECIMAL(5,2)      NOT NULL DEFAULT (0),
        SubTotal       DECIMAL(12,2)     NOT NULL DEFAULT (0),
        CgstAmount     DECIMAL(12,2)     NOT NULL DEFAULT (0),
        SgstAmount     DECIMAL(12,2)     NOT NULL DEFAULT (0),
        GrandTotal     DECIMAL(12,2)     NOT NULL DEFAULT (0),
        Status         NVARCHAR(20)      NOT NULL DEFAULT ('Pending'),
        Notes          NVARCHAR(500)     NULL,
        CreatedAt      DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt      DATETIME2         NULL,

        CONSTRAINT UQ_Invoices_InvoiceNumber UNIQUE (InvoiceNumber),
        CONSTRAINT FK_Invoices_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (CustomerId),
        CONSTRAINT CK_Invoices_Status CHECK (Status IN ('Pending', 'Partial', 'Paid', 'Cancelled'))
    );

    CREATE INDEX IX_Invoices_InvoiceDate ON dbo.Invoices (InvoiceDate);
    CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices (CustomerId);
END
GO

IF OBJECT_ID('dbo.InvoiceItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems
    (
        InvoiceItemId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId     INT               NOT NULL,
        ServiceId     INT               NULL,
        SNo           INT               NOT NULL,
        Particular    NVARCHAR(300)     NOT NULL,
        Qty           DECIMAL(10,2)     NOT NULL,
        Rate          DECIMAL(10,2)     NOT NULL,
        Amount        DECIMAL(12,2)     NOT NULL,

        CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (InvoiceId)
            REFERENCES dbo.Invoices (InvoiceId) ON DELETE CASCADE,
        CONSTRAINT FK_InvoiceItems_Services FOREIGN KEY (ServiceId)
            REFERENCES dbo.Services (ServiceId) ON DELETE SET NULL
    );

    CREATE INDEX IX_InvoiceItems_InvoiceId ON dbo.InvoiceItems (InvoiceId);
END
GO
