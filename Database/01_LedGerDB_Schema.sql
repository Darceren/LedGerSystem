/*
================================================================================
  LedGerDB - FX / Remittance Ledger Database Schema
  Project : LedGerSystem
  Version : 1.0.0
  Database: LedGerDB (SQL Server)

  Usage:
    1. Ensure database LedGerDB already exists.
    2. Execute this script in SSMS / Azure Data Studio.
    3. Then execute 02_LedGerDB_SeedData.sql
    4. Then execute 04_LedGerDB_ColumnDescriptions.sql

  Notes:
    - Amount fields use DECIMAL(18,4); rates use DECIMAL(18,6).
    - Business day cutoff: natural day 00:00 (use TransDate / TransDateOnly).
    - Balances are derived from OpeningBalance + LedgerTransaction (Service layer).
================================================================================
*/

USE [LedGerDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* ============================================================================
   DROP (child tables first) — comment out on production if tables have data
   ============================================================================ */
IF OBJECT_ID(N'dbo.LedgerTransaction', N'U') IS NOT NULL DROP TABLE dbo.LedgerTransaction;
IF OBJECT_ID(N'dbo.OpeningBalance', N'U') IS NOT NULL DROP TABLE dbo.OpeningBalance;
IF OBJECT_ID(N'dbo.BankAccount', N'U') IS NOT NULL DROP TABLE dbo.BankAccount;
IF OBJECT_ID(N'dbo.Party', N'U') IS NOT NULL DROP TABLE dbo.Party;
IF OBJECT_ID(N'dbo.Sys_TransactionType', N'U') IS NOT NULL DROP TABLE dbo.Sys_TransactionType;
IF OBJECT_ID(N'dbo.Sys_User', N'U') IS NOT NULL DROP TABLE dbo.Sys_User;
GO

/* ============================================================================
   1. Sys_User — system login users
   ============================================================================ */
CREATE TABLE dbo.Sys_User
(
    Id              BIGINT          NOT NULL IDENTITY(1,1),
    UserName        NVARCHAR(50)    NOT NULL,
    PasswordHash    NVARCHAR(256)   NOT NULL,
    PasswordSalt    NVARCHAR(64)    NOT NULL,
    DisplayName     NVARCHAR(100)   NOT NULL,
    IsActive        BIT             NOT NULL CONSTRAINT DF_Sys_User_IsActive DEFAULT (1),
    LastLoginAt     DATETIME2(0)    NULL,
    CreatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_Sys_User_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt       DATETIME2(0)    NOT NULL CONSTRAINT DF_Sys_User_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Sys_User PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Sys_User_UserName UNIQUE (UserName)
);
GO

CREATE NONCLUSTERED INDEX IX_Sys_User_IsActive ON dbo.Sys_User (IsActive);
GO

/* ============================================================================
   2. Sys_TransactionType — transaction type dictionary
   ============================================================================ */
CREATE TABLE dbo.Sys_TransactionType
(
    Id                  INT             NOT NULL IDENTITY(1,1),
    Code                VARCHAR(40)     NOT NULL,
    NameEn              NVARCHAR(100)   NOT NULL,
    NameZh              NVARCHAR(100)   NULL,
    Category            TINYINT         NOT NULL,   /* 1=Inbound 2=Outbound 3=Transfer 4=System */
    AffectsCash         BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_AffectsCash DEFAULT (0),
    AffectsBank         BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_AffectsBank DEFAULT (0),
    AffectsPartyBalance BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_AffectsPartyBalance DEFAULT (1),
    RequireParty        BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_RequireParty DEFAULT (0),
    RequireRelatedParty BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_RequireRelatedParty DEFAULT (0),
    RequireBankAccount  BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_RequireBankAccount DEFAULT (0),
    RequirePaymentMode  BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_RequirePaymentMode DEFAULT (0),
    RequirePayoutChannel BIT            NOT NULL CONSTRAINT DF_Sys_TransactionType_RequirePayoutChannel DEFAULT (0),
    SortOrder           INT             NOT NULL CONSTRAINT DF_Sys_TransactionType_SortOrder DEFAULT (0),
    IsActive            BIT             NOT NULL CONSTRAINT DF_Sys_TransactionType_IsActive DEFAULT (1),
    Remark              NVARCHAR(200)   NULL,
    CONSTRAINT PK_Sys_TransactionType PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Sys_TransactionType_Code UNIQUE (Code),
    CONSTRAINT CK_Sys_TransactionType_Category CHECK (Category IN (1, 2, 3, 4))
);
GO

/* ============================================================================
   3. Party — customers, suppliers, Shufen, and other freely maintained parties
   ============================================================================ */
CREATE TABLE dbo.Party
(
    Id                  BIGINT          NOT NULL IDENTITY(1,1),
    Code                NVARCHAR(50)    NOT NULL,
    Name                NVARCHAR(200)   NOT NULL,
    PartyType           TINYINT         NOT NULL,   /* 1=Customer 2=UsdtSupplier 3=Shufen 4=Other */
    FinalAccountSide    TINYINT         NOT NULL CONSTRAINT DF_Party_FinalAccountSide DEFAULT (0),
    /* 0=Not shown 1=Credit(Receivable/Asset) 2=Debit(Payable/Liability) */
    ContactPhone        NVARCHAR(50)    NULL,
    Remark              NVARCHAR(500)   NULL,
    IsActive            BIT             NOT NULL CONSTRAINT DF_Party_IsActive DEFAULT (1),
    SortOrder           INT             NOT NULL CONSTRAINT DF_Party_SortOrder DEFAULT (0),
    CreatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_Party_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_Party_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Party PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Party_Code UNIQUE (Code),
    CONSTRAINT CK_Party_PartyType CHECK (PartyType IN (1, 2, 3, 4)),
    CONSTRAINT CK_Party_FinalAccountSide CHECK (FinalAccountSide IN (0, 1, 2))
);
GO

CREATE NONCLUSTERED INDEX IX_Party_PartyType_IsActive ON dbo.Party (PartyType, IsActive);
CREATE NONCLUSTERED INDEX IX_Party_FinalAccountSide ON dbo.Party (FinalAccountSide) WHERE FinalAccountSide IN (1, 2);
GO

/* ============================================================================
   4. BankAccount — operator bank accounts for receiving/sending BDT
   ============================================================================ */
CREATE TABLE dbo.BankAccount
(
    Id                  INT             NOT NULL IDENTITY(1,1),
    Code                NVARCHAR(50)    NOT NULL,
    Name                NVARCHAR(200)   NOT NULL,
    BankName            NVARCHAR(200)   NULL,
    AccountNo           NVARCHAR(100)   NULL,
    Currency            VARCHAR(10)     NOT NULL CONSTRAINT DF_BankAccount_Currency DEFAULT ('BDT'),
    FinalAccountSide    TINYINT         NOT NULL CONSTRAINT DF_BankAccount_FinalAccountSide DEFAULT (1),
    IsActive            BIT             NOT NULL CONSTRAINT DF_BankAccount_IsActive DEFAULT (1),
    SortOrder           INT             NOT NULL CONSTRAINT DF_BankAccount_SortOrder DEFAULT (0),
    Remark              NVARCHAR(500)   NULL,
    CreatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_BankAccount_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_BankAccount_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_BankAccount PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_BankAccount_Code UNIQUE (Code),
    CONSTRAINT CK_BankAccount_FinalAccountSide CHECK (FinalAccountSide IN (0, 1, 2)),
    CONSTRAINT CK_BankAccount_Currency CHECK (Currency IN ('BDT', 'CNY', 'USDT'))
);
GO

CREATE NONCLUSTERED INDEX IX_BankAccount_IsActive_SortOrder ON dbo.BankAccount (IsActive, SortOrder);
GO

/* ============================================================================
   5. OpeningBalance — go-live / manual opening balances
   ============================================================================ */
CREATE TABLE dbo.OpeningBalance
(
    Id                  BIGINT          NOT NULL IDENTITY(1,1),
    BalanceDate         DATE            NOT NULL,
    EntityType          TINYINT         NOT NULL,   /* 1=Party 2=BankAccount 3=Cash */
    PartyId             BIGINT          NULL,
    BankAccountId       INT             NULL,
    Currency            VARCHAR(10)     NOT NULL,
    Amount              DECIMAL(18, 4)  NOT NULL,
    /* Signed amount. Positive=receivable/asset; negative=payable/liability (see docs). */
    Remark              NVARCHAR(500)   NULL,
    CreatedByUserId     BIGINT          NULL,
    CreatedAt           DATETIME2(0)    NOT NULL CONSTRAINT DF_OpeningBalance_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_OpeningBalance PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_OpeningBalance_EntityType CHECK (EntityType IN (1, 2, 3)),
    CONSTRAINT CK_OpeningBalance_Currency CHECK (Currency IN ('BDT', 'CNY', 'USDT')),
    CONSTRAINT CK_OpeningBalance_EntityRef CHECK (
        (EntityType = 1 AND PartyId IS NOT NULL AND BankAccountId IS NULL) OR
        (EntityType = 2 AND BankAccountId IS NOT NULL AND PartyId IS NULL) OR
        (EntityType = 3 AND PartyId IS NULL AND BankAccountId IS NULL)
    ),
    CONSTRAINT FK_OpeningBalance_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party (Id),
    CONSTRAINT FK_OpeningBalance_BankAccount FOREIGN KEY (BankAccountId) REFERENCES dbo.BankAccount (Id),
    CONSTRAINT FK_OpeningBalance_User FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Sys_User (Id)
);
GO

CREATE NONCLUSTERED INDEX IX_OpeningBalance_BalanceDate ON dbo.OpeningBalance (BalanceDate);
CREATE NONCLUSTERED INDEX IX_OpeningBalance_Entity ON dbo.OpeningBalance (EntityType, PartyId, BankAccountId, Currency);
GO

/* ============================================================================
   6. LedgerTransaction — core ledger entries (all business flows)
   ============================================================================ */
CREATE TABLE dbo.LedgerTransaction
(
    Id                      BIGINT          NOT NULL IDENTITY(1,1),
    TransNo                 VARCHAR(32)     NULL,
    TransDate               DATETIME2(0)    NOT NULL,
    TransDateOnly           AS CAST(TransDate AS DATE) PERSISTED,
    TransTypeId             INT             NOT NULL,

    PartyId                 BIGINT          NULL,       /* customer / supplier / Shufen */
    RelatedPartyId          BIGINT          NULL,       /* triangular settlement supplier, etc. */
    BankAccountId           INT             NULL,

    Amount                  DECIMAL(18, 4)  NOT NULL CONSTRAINT CK_LedgerTransaction_Amount CHECK (Amount > 0),
    Currency                VARCHAR(10)     NOT NULL,

    UnitPrice               DECIMAL(18, 6)  NULL,       /* exchange rate / unit price */
    Quantity                DECIMAL(18, 6)  NULL,       /* e.g. USDT quantity */
    QuantityCurrency        VARCHAR(10)     NULL,

    EquivalentAmount        DECIMAL(18, 4)  NULL,       /* converted amount, e.g. expected BDT */
    EquivalentCurrency      VARCHAR(10)     NULL,

    PaymentMode             TINYINT         NULL,       /* 1=Cash 2=MyBank 3=SupplierBank */
    PayMethod               TINYINT         NULL,       /* 1=Cash 2=Bank (pay supplier) */
    PayoutChannel           TINYINT         NULL,       /* 1=Alipay 2=WeChat 3=Bank (sell RMB) */
    PayoutAccountName       NVARCHAR(100)   NULL,
    PayoutAccountNo         NVARCHAR(100)   NULL,

    IsOpening               BIT             NOT NULL CONSTRAINT DF_LedgerTransaction_IsOpening DEFAULT (0),
    IsAdjustment            BIT             NOT NULL CONSTRAINT DF_LedgerTransaction_IsAdjustment DEFAULT (0),
    ReversedTransId         BIGINT          NULL,
    BatchNo                 VARCHAR(32)     NULL,       /* group multiple lines same batch */

    Remark                  NVARCHAR(500)   NULL,
    CreatedByUserId         BIGINT          NULL,
    CreatedAt               DATETIME2(0)    NOT NULL CONSTRAINT DF_LedgerTransaction_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt               DATETIME2(0)    NOT NULL CONSTRAINT DF_LedgerTransaction_UpdatedAt DEFAULT (SYSUTCDATETIME()),
    IsDeleted               BIT             NOT NULL CONSTRAINT DF_LedgerTransaction_IsDeleted DEFAULT (0),

    CONSTRAINT PK_LedgerTransaction PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_LedgerTransaction_TransNo UNIQUE (TransNo),
    CONSTRAINT CK_LedgerTransaction_Currency CHECK (Currency IN ('BDT', 'CNY', 'USDT')),
    CONSTRAINT CK_LedgerTransaction_QuantityCurrency CHECK (QuantityCurrency IS NULL OR QuantityCurrency IN ('BDT', 'CNY', 'USDT')),
    CONSTRAINT CK_LedgerTransaction_EquivalentCurrency CHECK (EquivalentCurrency IS NULL OR EquivalentCurrency IN ('BDT', 'CNY', 'USDT')),
    CONSTRAINT CK_LedgerTransaction_PaymentMode CHECK (PaymentMode IS NULL OR PaymentMode IN (1, 2, 3)),
    CONSTRAINT CK_LedgerTransaction_PayMethod CHECK (PayMethod IS NULL OR PayMethod IN (1, 2)),
    CONSTRAINT CK_LedgerTransaction_PayoutChannel CHECK (PayoutChannel IS NULL OR PayoutChannel IN (1, 2, 3)),
    CONSTRAINT FK_LedgerTransaction_Type FOREIGN KEY (TransTypeId) REFERENCES dbo.Sys_TransactionType (Id),
    CONSTRAINT FK_LedgerTransaction_Party FOREIGN KEY (PartyId) REFERENCES dbo.Party (Id),
    CONSTRAINT FK_LedgerTransaction_RelatedParty FOREIGN KEY (RelatedPartyId) REFERENCES dbo.Party (Id),
    CONSTRAINT FK_LedgerTransaction_BankAccount FOREIGN KEY (BankAccountId) REFERENCES dbo.BankAccount (Id),
    CONSTRAINT FK_LedgerTransaction_Reversed FOREIGN KEY (ReversedTransId) REFERENCES dbo.LedgerTransaction (Id),
    CONSTRAINT FK_LedgerTransaction_User FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Sys_User (Id)
);
GO

/* Primary query indexes */
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_TransDateOnly ON dbo.LedgerTransaction (TransDateOnly DESC, Id DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_Type_Date ON dbo.LedgerTransaction (TransTypeId, TransDateOnly DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_Party_Date ON dbo.LedgerTransaction (PartyId, TransDateOnly DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_RelatedParty ON dbo.LedgerTransaction (RelatedPartyId) WHERE IsDeleted = 0 AND RelatedPartyId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_BankAccount_Date ON dbo.LedgerTransaction (BankAccountId, TransDateOnly DESC) WHERE IsDeleted = 0 AND BankAccountId IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_PaymentMode ON dbo.LedgerTransaction (PaymentMode, TransDateOnly DESC) WHERE IsDeleted = 0;
CREATE NONCLUSTERED INDEX IX_LedgerTransaction_BatchNo ON dbo.LedgerTransaction (BatchNo) WHERE BatchNo IS NOT NULL;
GO

PRINT 'Schema created successfully.';
PRINT 'Next: execute 02_LedGerDB_SeedData.sql then 04_LedGerDB_ColumnDescriptions.sql for full field descriptions.';
GO
