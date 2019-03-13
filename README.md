# EF Core Initialization

* The way EF Core initialization/destruction (context.Database.EnsureCreated & context.Database.EnsureDeleted) has been developed is that it assumes each DbContext uses a seperate physical data store. 
* This assumption is probably correct for production but when developing/prototyping I like to have multiple DbContexts using the same physical localDB/Sqlite data store.
* I have created the following extension methods which aims to allow each DbContext to be initialized & destroyed independently based on its Model. This also allows for a DbContent to switch from EnsureCreated() and Migrate() easily.
* Generally in staging/production environments all contexts will call context.Database.Migrate() which automatically allows for multiple DbContexts to use the same physical sql data store.

## Usage
* context.EnsureTablesAndMigrationsDeletedAsync() = Ensures only tables and migrations related to this context are deleted.
* context.EnsureDbAndTablesCreatedAsync() = Ensures Db created and only tables related to this context are created.
* context.ClearData() = Only data related to this context is deleted.
* Ensure Language Version is set to 'C# latest minor version (latest) to allow async Main.
* Ensure Main method is async.
```
 public class Program
{
	public static async Task Main(string[] args)
	{
		
	}
}
```

## Development Environment Example - Ideal for Rapid Prototyping
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=DevDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
var sqliteConnectionString = "Data Source=DevDatabase.db;";

Execution 1: Delete and Create for all contexts. Ideal for rapid prototyping.
await dataContext.EnsureTablesAndMigrationsDeletedAsync();
await dataContext.EnsureDbAndTablesCreatedAsync();

await identityContext.EnsureTablesAndMigrationsDeletedAsync();
await identityContext.EnsureDbAndTablesCreatedAsync();

await cmsContext.EnsureTablesAndMigrationsDeletedAsync();
await cmsContext.EnsureDbAndTablesCreatedAsync();
```

## Integration Environment Example
```
var sqlServerConnectionString = "Server=(localdb)\\mssqllocaldb;Database=IntegrationDatabase;Trusted_Connection=True;MultipleActiveResultSets=true;";
var sqliteConnectionString = "Data Source=IntegrationDatabase.db;";

await dataContext.EnsureTablesAndMigrationsDeletedAsync();
await dataContext.Database.MigrateAsync();

await identityContext.EnsureTablesAndMigrationsDeletedAsync();
await identityContext.Database.MigrateAsync();

await cmsContext.EnsureTablesAndMigrationsDeletedAsync();
await cmsContext.Database.MigrateAsync();
```

## Staging/Production Example
```
var sqlServerConnectionString = "Server=Server;Database=ProductionDatabase;Username=XXX;Password=XXX;MultipleActiveResultSets=true;";

await dataContext.Database.MigrateAsync();
await identityContext.Database.MigrateAsync();
await cmsContext.Database.MigrateAsync();
```

## Notes
* The EnsureTablesAndMigrationsDeletedAsync() method won't drop tables for Entities that have been deleted from the Context but for development/prototyping this is Ok.

## SQL Server and SQLite Migrations
* To prevent having to [add additional migration annotations manually](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers) so migrations work for SQL Server and SQLite the following IMigrationsAnnotationProvider can be used to automatically generate both.

```
public class Context : DbContext
{
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.ReplaceService<IMigrationsAnnotationProvider, RelationalMigrationsAnnotationsProvider>();
	}
}
```