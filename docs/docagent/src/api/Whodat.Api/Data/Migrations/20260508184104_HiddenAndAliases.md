# HiddenAndAliases

> **File:** `src/api/Whodat.Api/Data/Migrations/20260508184104_HiddenAndAliases.cs`  
> **Kind:** class

Adds a per-user hidden flag and a dedicated alias table to support user aliasing and visibility controls.

## Remarks

- The Up method extends the schema by adding IsHidden (non-nullable, default false) to AspNetUsers to indicate whether a user should be hidden from UI or listings.
- It also creates UserAliases with an autoincrement Id as the primary key, a required UserId foreign key to AspNetUsers, and an Alias field (TEXT, max length 32) that must be unique across all users. The FK enforces referential integrity and uses cascade delete so removing a user also removes their aliases.
- An index on Alias enforces global uniqueness to prevent alias collisions, while an index on UserId supports efficient queries by user.
- The migration relies on SQLite type mappings (INTEGER for numbers, TEXT for strings) and uses Sqlite:Autoincrement for the Id column.
- Down symmetry drops the UserAliases table before removing IsHidden from AspNetUsers, restoring the previous schema.
- This migration implicitly assumes that existing data in AspNetUsers will have IsHidden set to false by default and that alias data will be populated via application logic after migration; the unique constraint on Alias will reject duplicates across all users.
- Dependencies referenced by this migration include Migration (the EF Core migration base class) and ReferentialAction (for the cascade behavior on delete).

## Example

```csharp
using var context = new ApplicationDbContext(options);
context.Database.Migrate(); // Applies all pending migrations, including HiddenAndAliases
```

## Notes
- IsHidden is non-nullable and defaults to false for existing users during migration.
- Alias column enforces a strict maximum length of 32 characters.
- The unique constraint on Alias prevents two users from sharing the same alias across the entire table.
- Deleting a user will cascade and remove their associated aliases.
- Down migration removes the aliases table before removing the IsHidden column to preserve referential integrity during rollback.
