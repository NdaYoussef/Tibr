## Plan: Add Domain Entities and Initial EF Core Migration

TL;DR - Create the requested entity classes in `Tibr.Domain/Entities`, configure `ApplicationDbContext` in `Tibr.Infrastructure/Contexts`, add EF Core packages and registration, then run `dotnet ef migrations add InitialCreate` followed by `dotnet ef database update`.

**Steps**

1. Add entity classes in `Tibr.Domain/Entities` and inherit `BaseEntity<long>` for shared identity and audit fields.
   - Create classes: `User`, `Admin`, `KYCDocument`, `Product`, `Category`, `Favorite`, `Cart`, `CartItem`, `Order`, `OrderItem`, `Payment`, `Notification`, `SupportTicket`, `TicketReply`, `AuditLog`.
   - Use property types matching the schema: `long` for `bigint`, `string` for text fields, `bool` for flags, `decimal` for currency/purity/weight, `DateTime` for timestamps, `int` for quantity.
   - Add navigation properties for foreign keys and collection relationships to support EF Core.
   - Use the generic `BaseEntity<long>` to provide `Id`, `CreatedAt`, `UpdatedAt`, and `IsDeleted` consistently across entities.

2. Implement `ApplicationDbContext` in `Tibr.Infrastructure/Contexts/ApplicationDbContext.cs`.
   - Derive from `DbContext`.
   - Add `DbSet<TEntity>` properties for all new entity types.
   - Add constructor accepting `DbContextOptions<ApplicationDbContext>`.
   - Configure relationships in `OnModelCreating` if needed to enforce FK names and navigation properties.

3. Add EF Core packages to `Tibr.Infrastructure/Tibr.Infrastructure.csproj`.
   - At minimum: `Microsoft.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, and one provider package such as `Microsoft.EntityFrameworkCore.SqlServer`.
   - If a different provider is required, substitute `Microsoft.EntityFrameworkCore.SqlServer` with `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Npgsql`, etc.

4. Register the DbContext in startup.
   - Update `Tibr.API/Program.cs` or `Tibr.Infrastructure/DependencyInjection.cs` to call `AddDbContext<ApplicationDbContext>`.
   - Add a connection string to `Tibr.API/appsettings.json`.
   - Use `options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))` if using SQL Server.

5. Create the initial migration.
   - Ensure `dotnet-ef` is available: `dotnet tool install --global dotnet-ef` if needed.
   - Restore packages: `dotnet restore` from repo root.
   - Create migration: `dotnet ef migrations add InitialCreate --project Tibr.Infrastructure --startup-project Tibr.API --context ApplicationDbContext`.
   - Apply migration: `dotnet ef database update --project Tibr.Infrastructure --startup-project Tibr.API --context ApplicationDbContext`.

**Relevant files**

- `Tibr.Domain/Entities` — add new entity classes.
- `Tibr.Infrastructure/Contexts/ApplicationDbContext.cs` — add DbContext and DbSets.
- `Tibr.Infrastructure/Tibr.Infrastructure.csproj` — add EF Core package references.
- `Tibr.API/Program.cs` — register the DbContext.
- `Tibr.API/appsettings.json` — add connection string.

**Verification**

1. Build the solution to ensure the new entity classes compile.
2. Run the EF Core migration command and confirm an `Migrations` folder is created in `Tibr.Infrastructure`.
3. Confirm the database is created and schema matches the entity relationships after `dotnet ef database update`.
4. Optionally inspect generated SQL or use a database browser to ensure tables and foreign keys match the requested schema.
