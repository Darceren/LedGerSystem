/*
================================================================================
  LedGerDB - Seed Data
  Execute AFTER 01_LedGerDB_Schema.sql

  Default user:
    UserName  : shamim
    Password  : MUST be set on first app login (hash placeholder below)
================================================================================
*/

USE [LedGerDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

/* ============================================================================
   Transaction Types
   ============================================================================ */
SET IDENTITY_INSERT dbo.Sys_TransactionType ON;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 1)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (1, 'BUY_USDT', 'Buy USDT', N'Buy USDT', 3, 0, 0, 1, 1, 0, 0, 0, 0, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 2)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (2, 'USDT_TO_RMB', 'USDT to RMB', N'USDT to RMB', 3, 0, 0, 1, 1, 0, 0, 0, 0, 20);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 3)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (3, 'SELL_RMB', 'Sell RMB', N'Sell RMB', 2, 0, 0, 1, 1, 0, 0, 0, 1, 30);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 4)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (4, 'COLLECT_BDT_CASH', 'Collect BDT (Cash)', N'Collect BDT Cash', 1, 1, 0, 1, 1, 0, 0, 1, 0, 40);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 5)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (5, 'COLLECT_BDT_BANK', 'Collect BDT (My Bank)', N'Collect BDT My Bank', 1, 0, 1, 1, 1, 0, 1, 1, 0, 50);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 6)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (6, 'COLLECT_BDT_SUPPLIER', 'Collect BDT (Supplier Bank)', N'Collect BDT Supplier Bank', 1, 0, 0, 1, 1, 1, 0, 1, 0, 60);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 7)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (7, 'PAY_BDT_SUPPLIER', 'Pay BDT to Supplier', N'Pay BDT Supplier', 2, 1, 1, 1, 1, 0, 0, 0, 0, 70);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 8)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (8, 'EXPENSE', 'Expense', N'Expense', 2, 1, 1, 0, 0, 0, 0, 0, 0, 80);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 9)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (9, 'OPENING_BALANCE', 'Opening Balance', N'Opening Balance', 4, 1, 1, 1, 0, 0, 0, 0, 0, 90);

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_TransactionType WHERE Id = 10)
INSERT INTO dbo.Sys_TransactionType (Id, Code, NameEn, NameZh, Category, AffectsCash, AffectsBank, AffectsPartyBalance, RequireParty, RequireRelatedParty, RequireBankAccount, RequirePaymentMode, RequirePayoutChannel, SortOrder)
VALUES (10, 'ADJUSTMENT', 'Adjustment', N'Adjustment', 4, 1, 1, 1, 0, 0, 0, 0, 0, 100);

SET IDENTITY_INSERT dbo.Sys_TransactionType OFF;
GO

/* ============================================================================
   Default user (password set by application on first run)
   ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Sys_User WHERE UserName = N'shamim')
BEGIN
    INSERT INTO dbo.Sys_User (UserName, PasswordHash, PasswordSalt, DisplayName, IsActive)
    VALUES (N'shamim', N'PENDING_SET_ON_FIRST_LOGIN', N'PENDING', N'Shamim', 1);
END
GO

/* ============================================================================
   Sample suppliers from questionnaire (optional starter data)
   ============================================================================ */
IF NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Code = N'SKT')
    INSERT INTO dbo.Party (Code, Name, PartyType, FinalAccountSide, SortOrder)
    VALUES (N'SKT', N'skt express', 2, 2, 10);

IF NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Code = N'KSM')
    INSERT INTO dbo.Party (Code, Name, PartyType, FinalAccountSide, SortOrder)
    VALUES (N'KSM', N'ksm express', 2, 2, 20);

IF NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Code = N'AMT')
    INSERT INTO dbo.Party (Code, Name, PartyType, FinalAccountSide, SortOrder)
    VALUES (N'AMT', N'amt express', 2, 2, 30);

IF NOT EXISTS (SELECT 1 FROM dbo.Party WHERE Code = N'SHUFEN')
    INSERT INTO dbo.Party (Code, Name, PartyType, FinalAccountSide, SortOrder)
    VALUES (N'SHUFEN', N'Shufen', 3, 0, 40);
GO

PRINT 'Seed data applied successfully.';
GO
