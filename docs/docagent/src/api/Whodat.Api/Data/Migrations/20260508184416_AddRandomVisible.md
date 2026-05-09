# AddRandomVisible

> **File:** `src/api/Whodat.Api/Data/Migrations/20260508184416_AddRandomVisible.cs`  
> **Kind:** class

```csharp
/// <inheritdoc />
    public partial class AddRandomVisible : Migration
```


This EF Core migration adds a new non-nullable boolean column named RandomVisible to the AspNetUsers table. The column is stored as an INTEGER in the database, with a default value of false; the Up method applies the change and the Down method removes the column to roll back. Use this migration when you need a per-user RandomVisible flag with a safe default and the ability to revert the schema change.

## Remarks
- The column uses the database type INTEGER to represent the boolean value.
- Defaults to false for existing and new users due to defaultValue: false. 
- Down reverses the change by dropping the RandomVisible column from AspNetUsers.