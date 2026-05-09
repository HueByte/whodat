# AddRandomVisible

> **File:** `src/api/Whodat.Api/Data/Migrations/20260508184416_AddRandomVisible.Designer.cs`  
> **Kind:** class

This migration designer defines the target model for the 20260508184416_AddRandomVisible migration in the WhodatDb context. It provides EF Core with a snapshot of the database schema after applying this migration so migrations can be composed and applied correctly.

## Remarks

The BuildTargetModel method uses the provided ModelBuilder to declare the database model that should exist after this migration is applied. The snapshot shown configures identity-related tables such as AspNetRoles, AspNetRoleClaims, AspNetUserClaims, AspNetUserLogins, and AspNetUserRoles, including their keys, indices, and column types (provider-specific mappings like TEXT and INTEGER). The call is wrapped with pragma warnings to suppress obsolete/compatibility warnings, and a ProductVersion annotation records the EF Core tooling version at scaffold time. This class is designer-generated and is intended to assist EF Core in diffing models across migrations; it is not intended for manual editing.

Keep in mind that while this file describes the target state of the model, the concrete operations that alter the database (creating, altering, or dropping tables) are defined in the migration’s Up/Down methods in the corresponding non-designer file.

## Example

```csharp
// Example usage: migrate the database to the target migration programmatically
using var db = new WhodatDb();
var migrator = db.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
await migrator.MigrateAsync("20260508184416_AddRandomVisible");
```

## Notes

- This class is a designer-generated snapshot used by EF Core migrations; avoid manual edits.
- The target model reflects provider-specific column types (e.g., TEXT, INTEGER) appropriate to the active database provider.
- The presence of identity-related tables in the snapshot indicates the current authentication/authorization schema included in the model at this migration point.
- Up/Down migration logic resides in the corresponding migration file, while this designer file solely captures the resulting model state.