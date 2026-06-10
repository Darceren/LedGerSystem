# LedGerSystem

.NET 8 MVC + SQL Server + SqlSugar FX ledger for shamim.

## Run

```powershell
cd D:\MyProject\GameProject\LedGerSystem\LedGerSystem
dotnet run
```

Browser: https://localhost:5xxx (see console output)

## Login

| Field | Value |
|-------|-------|
| Username | `shamim` |
| Password | `ChangeMe123!` (auto-set on first run if DB has placeholder) |

## Database

1. Execute `Database/01_LedGerDB_Schema.sql`
2. Execute `Database/02_LedGerDB_SeedData.sql`
3. Execute `Database/04_LedGerDB_ColumnDescriptions.sql`

Connection string: `appsettings.Development.json` → `ConnectionStrings:LedGerDB`

## Project Structure

```
Entities/          SqlSugar entities
Infrastructure/    SqlSugar, password helper
Services/          Auth, lookup, transaction
Controllers/       Account, Entry, Home
Views/Entry/       Mobile-first transaction entry
Database/          SQL scripts
```

## First Entry

1. Login
2. Go to **New Entry**
3. Default type: **Collect BDT (Cash)**
4. Select customer **NYMT** (auto-created if none)
5. Enter amount and save
