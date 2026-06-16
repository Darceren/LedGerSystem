/*
  Dev/test opening balances — safe to re-run (deletes prior [DEV] rows only).
  Execute in SSMS after 01/02 scripts. Remark contains [DEV] for identification.
*/
USE [LedGerDB];
GO

SET NOCOUNT ON;

DECLARE @GoLive DATE = '2026-06-01';
DECLARE @UserId BIGINT = (SELECT Id FROM dbo.Sys_User WHERE UserName = N'shamim');

IF @UserId IS NULL
BEGIN
    RAISERROR('User shamim not found. Run 02_LedGerDB_SeedData.sql first.', 16, 1);
    RETURN;
END

DELETE FROM dbo.OpeningBalance WHERE Remark LIKE N'%[DEV]%';
GO

DECLARE @GoLive DATE = '2026-06-01';
DECLARE @UserId BIGINT = (SELECT Id FROM dbo.Sys_User WHERE UserName = N'shamim');

-- Sample banks for dev (if missing)
IF NOT EXISTS (SELECT 1 FROM dbo.BankAccount WHERE Code = N'BANK01')
    INSERT INTO dbo.BankAccount (Code, Name, BankName, Currency, FinalAccountSide, SortOrder)
    VALUES (N'BANK01', N'Main BDT Account', N'Dutch Bangla Bank', N'BDT', 1, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.BankAccount WHERE Code = N'BANK02')
    INSERT INTO dbo.BankAccount (Code, Name, BankName, Currency, FinalAccountSide, SortOrder)
    VALUES (N'BANK02', N'Secondary BDT Account', N'BRAC Bank', N'BDT', 1, 20);

-- Customer NYMT receivable +500,000 BDT
INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, PartyId, Currency, Amount, Remark, CreatedByUserId)
SELECT @GoLive, 1, p.Id, N'BDT', 500000, N'[DEV] NYMT customer receivable', @UserId
FROM dbo.Party p WHERE p.Code = N'NYMT';

-- Supplier SKT payable +200,000 BDT (positive = you owe supplier)
INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, PartyId, Currency, Amount, Remark, CreatedByUserId)
SELECT @GoLive, 1, p.Id, N'BDT', 200000, N'[DEV] SKT supplier payable', @UserId
FROM dbo.Party p WHERE p.Code = N'SKT';

-- Shufen net payable +50,000 BDT
INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, PartyId, Currency, Amount, Remark, CreatedByUserId)
SELECT @GoLive, 1, p.Id, N'BDT', 50000, N'[DEV] Shufen balance', @UserId
FROM dbo.Party p WHERE p.Code = N'SHUFEN';

-- Cash on hand 1,000,000 BDT
INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, Currency, Amount, Remark, CreatedByUserId)
VALUES (@GoLive, 3, N'BDT', 1000000, N'[DEV] cash on hand', @UserId);

-- Bank balances
INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, BankAccountId, Currency, Amount, Remark, CreatedByUserId)
SELECT @GoLive, 2, b.Id, N'BDT', 300000, N'[DEV] BANK01 opening', @UserId
FROM dbo.BankAccount b WHERE b.Code = N'BANK01';

INSERT INTO dbo.OpeningBalance (BalanceDate, EntityType, BankAccountId, Currency, Amount, Remark, CreatedByUserId)
SELECT @GoLive, 2, b.Id, N'BDT', 150000, N'[DEV] BANK02 opening', @UserId
FROM dbo.BankAccount b WHERE b.Code = N'BANK02';

PRINT 'Dev opening balances inserted. Count:';
SELECT COUNT(*) AS DevOpeningCount FROM dbo.OpeningBalance WHERE Remark LIKE N'%[DEV]%';
GO
