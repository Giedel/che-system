USE [ChemLab_DB];
GO

-- Step 1: Drop all existing FK constraints referencing User.id_number for user-related columns
-- Borrower_Slip
ALTER TABLE [dbo].[Borrower_Slip] DROP CONSTRAINT [FK__Borrower___recei__6383C8BA];
GO
ALTER TABLE [dbo].[Borrower_Slip] DROP CONSTRAINT [FK__Borrower___relea__6477ECF3];
GO
ALTER TABLE [dbo].[Borrower_Slip] DROP CONSTRAINT [FK__Borrower___check__628FA481];
GO

-- Return
ALTER TABLE [dbo].[Return] DROP CONSTRAINT [FK__Return__received__6C190EBB];
GO
ALTER TABLE [dbo].[Return] DROP CONSTRAINT [FK__Return__checked___6B24EA82];
GO

-- Step 2: Now update existing data to map id_number values to corresponding usernames, NULL unmatched
-- Borrower_Slip received_by
UPDATE bs SET bs.received_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.received_by = u.id_number
WHERE bs.received_by IS NOT NULL;
GO

UPDATE [dbo].[Borrower_Slip] SET received_by = NULL
WHERE received_by IS NOT NULL AND received_by NOT IN (SELECT id_number FROM [dbo].[User]);
GO

-- released_by
UPDATE bs SET bs.released_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.released_by = u.id_number
WHERE bs.released_by IS NOT NULL;
GO

UPDATE [dbo].[Borrower_Slip] SET released_by = NULL
WHERE released_by IS NOT NULL AND released_by NOT IN (SELECT id_number FROM [dbo].[User]);
GO

-- checked_by
UPDATE bs SET bs.checked_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.checked_by = u.id_number
WHERE bs.checked_by IS NOT NULL;
GO

UPDATE [dbo].[Borrower_Slip] SET checked_by = NULL
WHERE checked_by IS NOT NULL AND checked_by NOT IN (SELECT id_number FROM [dbo].[User]);
GO

-- Return received_by
UPDATE r SET r.received_by = u.username
FROM [dbo].[Return] r
INNER JOIN [dbo].[User] u ON r.received_by = u.id_number
WHERE r.received_by IS NOT NULL;
GO

UPDATE [dbo].[Return] SET received_by = NULL
WHERE received_by IS NOT NULL AND received_by NOT IN (SELECT id_number FROM [dbo].[User]);
GO

-- Return checked_by
UPDATE r SET r.checked_by = u.username
FROM [dbo].[Return] r
INNER JOIN [dbo].[User] u ON r.checked_by = u.id_number
WHERE r.checked_by IS NOT NULL;
GO

UPDATE [dbo].[Return] SET checked_by = NULL
WHERE checked_by IS NOT NULL AND checked_by NOT IN (SELECT id_number FROM [dbo].[User]);
GO

-- Step 3: Add new FK constraints referencing User.username
-- Borrower_Slip received_by
ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK ADD CONSTRAINT [FK_Borrower_Slip_received_by_User_username] 
FOREIGN KEY([received_by]) REFERENCES [dbo].[User] ([username]);
GO

-- released_by
ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK ADD CONSTRAINT [FK_Borrower_Slip_released_by_User_username] 
FOREIGN KEY([released_by]) REFERENCES [dbo].[User] ([username]);
GO

-- checked_by
ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK ADD CONSTRAINT [FK_Borrower_Slip_checked_by_User_username] 
FOREIGN KEY([checked_by]) REFERENCES [dbo].[User] ([username]);
GO

-- Return received_by
ALTER TABLE [dbo].[Return] WITH CHECK ADD CONSTRAINT [FK_Return_received_by_User_username] 
FOREIGN KEY([received_by]) REFERENCES [dbo].[User] ([username]);
GO

-- checked_by
ALTER TABLE [dbo].[Return] WITH CHECK ADD CONSTRAINT [FK_Return_checked_by_User_username] 
FOREIGN KEY([checked_by]) REFERENCES [dbo].[User] ([username]);
GO

-- Step 4: Verify new FKs
SELECT 
    fk.name AS constraint_name,
    tp.name AS table_name,
    cp.name AS column_name,
    tr.name AS referenced_table,
    cr.name AS referenced_column
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
WHERE tr.name = 'User' AND tr.schema_id = SCHEMA_ID('dbo')
AND tp.name IN ('Borrower_Slip', 'Return')
ORDER BY tp.name, cp.name;
GO
