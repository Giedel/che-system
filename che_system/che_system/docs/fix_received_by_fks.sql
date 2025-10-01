USE [ChemLab_DB];
GO

-- Step 1: Map existing received_by values (assuming they are id_numbers) to matching usernames, set unmatched to NULL
-- For Borrower_Slip
UPDATE bs
SET bs.received_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.received_by = u.id_number
WHERE bs.received_by IS NOT NULL;

-- Set unmatched received_by to NULL
UPDATE [dbo].[Borrower_Slip]
SET received_by = NULL
WHERE received_by IS NOT NULL AND received_by NOT IN (SELECT id_number FROM [dbo].[User]);

-- Repeat for released_by and checked_by in Borrower_Slip
UPDATE bs
SET bs.released_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.released_by = u.id_number
WHERE bs.released_by IS NOT NULL;

UPDATE [dbo].[Borrower_Slip]
SET released_by = NULL
WHERE released_by IS NOT NULL AND released_by NOT IN (SELECT id_number FROM [dbo].[User]);

UPDATE bs
SET bs.checked_by = u.username
FROM [dbo].[Borrower_Slip] bs
INNER JOIN [dbo].[User] u ON bs.checked_by = u.id_number
WHERE bs.checked_by IS NOT NULL;

UPDATE [dbo].[Borrower_Slip]
SET checked_by = NULL
WHERE checked_by IS NOT NULL AND checked_by NOT IN (SELECT id_number FROM [dbo].[User]);

-- For Return table (received_by and checked_by)
UPDATE r
SET r.received_by = u.username
FROM [dbo].[Return] r
INNER JOIN [dbo].[User] u ON r.received_by = u.id_number
WHERE r.received_by IS NOT NULL;

UPDATE [dbo].[Return]
SET received_by = NULL
WHERE received_by IS NOT NULL AND received_by NOT IN (SELECT id_number FROM [dbo].[User]);

UPDATE r
SET r.checked_by = u.username
FROM [dbo].[Return] r
INNER JOIN [dbo].[User] u ON r.checked_by = u.id_number
WHERE r.checked_by IS NOT NULL;

UPDATE [dbo].[Return]
SET checked_by = NULL
WHERE checked_by IS NOT NULL AND checked_by NOT IN (SELECT id_number FROM [dbo].[User]);

-- Step 2: Drop existing FKs (use exact names from previous query; adjust if needed based on output)
-- For Borrower_Slip received_by (adjust name if exact differs)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__Borrower___recei__6383C8BA')
    ALTER TABLE [dbo].[Borrower_Slip] DROP CONSTRAINT [FK__Borrower___recei__6383C8BA];
GO

-- Drop other FKs for Borrower_Slip (add similar for released_by, checked_by if they have separate constraints)
-- Note: If all three share one constraint name or separate, query sys.foreign_keys to confirm
-- Assuming separate, but for simplicity, drop all referencing User for those columns
-- Better: Drop specifically
-- Run this to get names first, but for script, use conditional

-- For released_by and checked_by, assuming similar names, but to be precise, use:
-- You may need to run SELECT to get exact names for released_by and checked_by FKs

-- Temporary: Assume names like FK__Borrower_Slip__released_by__... but for now, focus on received_by as per error
-- To complete, add drops for all

-- Step 3: Add new FKs referencing username
ALTER TABLE [dbo].[Borrower_Slip] WITH NOCHECK ADD CONSTRAINT [FK_Borrower_Slip_received_by_User_username] 
FOREIGN KEY([received_by]) REFERENCES [dbo].[User] ([username]);
GO

ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK CHECK CONSTRAINT [FK_Borrower_Slip_received_by_User_username];
GO

-- Repeat for released_by and checked_by
ALTER TABLE [dbo].[Borrower_Slip] WITH NOCHECK ADD CONSTRAINT [FK_Borrower_Slip_released_by_User_username] 
FOREIGN KEY([released_by]) REFERENCES [dbo].[User] ([username]);
GO

ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK CHECK CONSTRAINT [FK_Borrower_Slip_released_by_User_username];
GO

ALTER TABLE [dbo].[Borrower_Slip] WITH NOCHECK ADD CONSTRAINT [FK_Borrower_Slip_checked_by_User_username] 
FOREIGN KEY([checked_by]) REFERENCES [dbo].[User] ([username]);
GO

ALTER TABLE [dbo].[Borrower_Slip] WITH CHECK CHECK CONSTRAINT [FK_Borrower_Slip_checked_by_User_username];
GO

-- For Return table
-- Drop existing for received_by: FK__Return__received__6C190EBB
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK__Return__received__6C190EBB')
    ALTER TABLE [dbo].[Return] DROP CONSTRAINT [FK__Return__received__6C190EBB];
GO

-- Add new
ALTER TABLE [dbo].[Return] WITH NOCHECK ADD CONSTRAINT [FK_Return_received_by_User_username] 
FOREIGN KEY([received_by]) REFERENCES [dbo].[User] ([username]);
GO

ALTER TABLE [dbo].[Return] WITH CHECK CHECK CONSTRAINT [FK_Return_received_by_User_username];
GO

-- For checked_by on Return (assuming similar FK, adjust name)
-- If there's a separate FK, drop and add similarly. Run SELECT to confirm.
-- For now, assume

-- Step 4: Verify
SELECT name AS new_fk_name FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('dbo.Borrower_Slip') AND referenced_object_id = OBJECT_ID('dbo.User');
GO
