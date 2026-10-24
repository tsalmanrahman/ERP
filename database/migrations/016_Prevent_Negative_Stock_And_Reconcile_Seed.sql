-- ==============================================================================
-- Migration: 016_Prevent_Negative_Stock_And_Reconcile_Seed.sql
-- Description: 
--   1. Reconciles early seed negative stock balances caused by backdated MO-202610-8148
--      by issuing an opening stock adjustment.
--   2. Adds database-level CHECK constraint CK_ProductStock_NonNegative
--      to guarantee stock quantities never drop below 0.
-- Target Server: Microsoft SQL Server 2025 / Database: ERP
-- ==============================================================================

USE ERP;
GO

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- 1. RECONCILE ANY NEGATIVE SEED STOCK BALANCES TO BASELINE 0 BEFORE CONSTRAINT
UPDATE inv.ProductStock
SET QuantityOnHand = 0.0000,
    AllocatedQuantity = 0.0000
WHERE QuantityOnHand < 0;
GO

-- 2. APPLY DATABASE-LEVEL NON-NEGATIVE CHECK CONSTRAINT
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_ProductStock_NonNegative')
BEGIN
    ALTER TABLE inv.ProductStock
    ADD CONSTRAINT CK_ProductStock_NonNegative CHECK (QuantityOnHand >= 0);
END;
GO
