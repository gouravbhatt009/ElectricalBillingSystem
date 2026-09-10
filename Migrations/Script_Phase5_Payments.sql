-- =====================================================================
-- Phase 5 schema additions: Payments
-- Run this after Script_Phase1_InitialCreate.sql, Script_Phase2, and
-- Script_Phase3 if you're using manual SQL setup instead of EF Core
-- migrations. Also add the CustomerId index to Invoices in case it was
-- created before this phase.
-- =====================================================================

USE ElectricalBillingDb;
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments
    (
        PaymentId       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        InvoiceId       INT               NOT NULL,
        Amount          DECIMAL(12,2)     NOT NULL,
        PaymentDate     DATETIME2         NOT NULL,
        PaymentMode     NVARCHAR(20)      NOT NULL DEFAULT ('Cash'),
        ReferenceNumber NVARCHAR(100)     NULL,
        Notes           NVARCHAR(500)     NULL,
        CreatedAt       DATETIME2         NOT NULL DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2         NULL,

        CONSTRAINT FK_Payments_Invoices FOREIGN KEY (InvoiceId)
            REFERENCES dbo.Invoices (InvoiceId) ON DELETE CASCADE,
        CONSTRAINT CK_Payments_Amount CHECK (Amount > 0),
        CONSTRAINT CK_Payments_PaymentMode CHECK (PaymentMode IN ('Cash', 'UPI', 'BankTransfer', 'Cheque', 'Other'))
    );

    CREATE INDEX IX_Payments_InvoiceId ON dbo.Payments (InvoiceId);
    CREATE INDEX IX_Payments_PaymentDate ON dbo.Payments (PaymentDate);
END
GO

-- Ensure the CustomerId index on Invoices exists (added in Phase 5 for
-- faster customer-wise reports; harmless no-op if already present).
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_Invoices_CustomerId' AND object_id = OBJECT_ID('dbo.Invoices')
)
BEGIN
    CREATE INDEX IX_Invoices_CustomerId ON dbo.Invoices (CustomerId);
END
GO
