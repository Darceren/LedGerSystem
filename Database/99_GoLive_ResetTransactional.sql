/*
  Go-live reset — removes ALL transactions and opening balances.
  Run only before entering real production data.
*/
USE [LedGerDB];
GO

SET NOCOUNT ON;

DELETE FROM dbo.LedgerTransaction;
DELETE FROM dbo.OpeningBalance;

DBCC CHECKIDENT ('dbo.LedgerTransaction', RESEED, 0);
DBCC CHECKIDENT ('dbo.OpeningBalance', RESEED, 0);

PRINT 'Transactional data cleared. Enter real opening balances before go-live.';
GO
