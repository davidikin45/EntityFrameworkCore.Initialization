﻿using Database.Initialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.EntityFrameworkCore
{
    public static class DbContextInitializationExtensions
    {
        #region ConnectionString
        public static string GetConnectionString(this DbContext context)
        {
            if (context.Database.IsSqlServer() || context.Database.IsSqlite())
            {
                var dbConnection = context.Database.GetDbConnection();
                var connectionString = dbConnection.ConnectionString;
                return connectionString;
            }
            else
            {
                return "";
            }
        }
        #endregion

        #region Ensure Db and Tables Created at a DbContext Level
        /// <summary>Adds objects that are used by the model for this context</summary>
        public static async Task<bool> EnsureDbAndTablesCreatedAsync(this DbContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            var created = await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
            if (!created)
            {
                //https://docs.microsoft.com/en-us/ef/core/managing-schemas/ensure-created
                //CreateTables would throw exception if any table already exists. 
                //var databaseCreator = dbContext.GetService<IRelationalDatabaseCreator>();
                //databaseCreator.CreateTables();

                if (context.Database.IsSqlServer() || context.Database.IsSqlite())
                {
                    var dependencies = context.Database.GetService<RelationalDatabaseCreatorDependencies>();

                    IReadOnlyList<MigrationCommand> createTablesCommands = dependencies.MigrationsSqlGenerator.Generate(dependencies.ModelDiffer.GetDifferences(null, dependencies.Model.GetRelationalModel()), dependencies.Model);

                    var persistedTables = await DbInitializer.TablesAsync(context.Database.GetDbConnection(), cancellationToken).ConfigureAwait(false);

                    var newTablesCommands = createTablesCommands.Where(command => !persistedTables.Any(persistedTable => command.CommandText.Contains(dependencies.SqlGenerationHelper.DelimitIdentifier(persistedTable.TableName, persistedTable.Schema)))).ToList();

                    if (newTablesCommands.Count() > 0)
                    {
                        await dependencies.MigrationCommandExecutor.ExecuteNonQueryAsync(newTablesCommands, dependencies.Connection, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                }
            }
            return created;
        }
        #endregion

        #region Delete Tables and Migrations DbContent Level
        /// <summary>Just removes the database objects that are used by the model for this context.</summary>
        /// <param name="context">The context.</param>
        public static async Task<bool> EnsureTablesAndMigrationsDeletedAsync(this DbContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool dbExists = false;
            try
            {
                if (await context.Database.ExistsAsync(cancellationToken).ConfigureAwait(false))
                    dbExists = true;
            }
            catch
            {

            }

            if (dbExists)
            {
                if (context.Database.IsSqlServer() || context.Database.IsSqlite())
                {
                    var modelTypes = context.GetModelTypes();

                    var tableNames = new List<(string Schema, string TableName)>();
                    foreach (var entityType in modelTypes)
                    {

                        var type = context.Model.FindEntityType(entityType);
                        var schema = type.GetSchema();
                        var tableName = type.GetTableName();

                        //.NET Core 2.2
                        //var mapping = context.Model.FindEntityType(entityType).Relational();
                        //var schema = mapping.Schema;
                        //var tableName = mapping.TableName;

                        tableNames.Add((schema, tableName));
                    }

                    var persistedTables = await DbInitializer.TablesAsync(context.Database.GetDbConnection(), cancellationToken).ConfigureAwait(false);

                    var commands = new List<string>();

                    var deleteTables = tableNames.Where(t => persistedTables.Any(pt => pt.TableName == t.TableName)).ToList();

                    //Drop tables
                    if (context.Database.IsSqlServer())
                    {
                        foreach (var tableName in deleteTables)
                        {
                            foreach (var t in deleteTables)
                            {
                                var schema = !string.IsNullOrEmpty(t.Schema) ? $"[{t.Schema}]." : "";
                                var table = $"[{t.TableName}]";

                                string command = $@"BEGIN TRY
                                                        DROP TABLE {schema}{table}
                                                        END TRY
                                                        BEGIN CATCH
                                                        END CATCH";

                                commands.Add(command);
                            }
                        }
                    }
                    else if (context.Database.IsSqlite())
                    {
                        foreach (var t in deleteTables)
                        {
                            var table = $"[{t.TableName}]";

                            string command = $"DROP TABLE {table}";

                            commands.Add(command);
                        }
                    }

                    //Delete migrations
                    if (await TableExistsAsync(context.Database, "__EFMigrationsHistory", cancellationToken))
                    {
                        foreach (var migrationId in context.Database.Migrations())
                        {
                            string command = $"DELETE FROM [__EFMigrationsHistory] WHERE MigrationId = '{migrationId}'";
                            commands.Add(command);
                        }
                    }

                    using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
                    {
                        foreach (var command in commands)
                        {
                            await context.Database.ExecuteSqlRawAsync(command, cancellationToken).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }

                    return true;
                }
                else if (context.Database.IsInMemory())
                {
                    return await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
                }

                return false;
            }
            else
            {
                //As long as the Db is online this will physically delete db.
                return await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
            }
        }
#endregion

#region Clear Data at a DbContext Level
        public static void ClearData(this DbContext context)
        {
            var modelTypes = context.GetModelTypes();
            foreach (var entityType in modelTypes)
            {
                var dbSet = Set(context, entityType);

                var paramType = typeof(IEnumerable<>).MakeGenericType(entityType);
                var removeRangeMethod = typeof(DbSet<>).MakeGenericType(entityType).GetMethod("RemoveRange", new[] { paramType });
                removeRangeMethod.Invoke(dbSet, new object[] { dbSet });
            }
        }

        static readonly MethodInfo SetMethod = typeof(DbContext).GetMethod(nameof(DbContext.Set));
        private static IQueryable Set(this DbContext context, Type entityType) =>
            (IQueryable)SetMethod.MakeGenericMethod(entityType).Invoke(context, null);
#endregion

#region Exists
        public static async Task<bool> ExistsAsync(this DatabaseFacade databaseFacade, CancellationToken cancellationToken = default(CancellationToken))
        {
            var relationalDatabaseCreator = databaseFacade.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (relationalDatabaseCreator != null)
            {
                return await relationalDatabaseCreator.ExistsAsync(cancellationToken).ConfigureAwait(false);

            }
            else
            {
                return true;
            }
        }
#endregion

#region Table Exists
        public static async Task<bool> TableExistsAsync(this DatabaseFacade databaseFacade, string tableName, CancellationToken cancellationToken = default(CancellationToken))
        {
            var relationalDatabaseCreator = databaseFacade.GetService<IDatabaseCreator>() as RelationalDatabaseCreator;
            if (relationalDatabaseCreator != null)
            {
                if (databaseFacade.IsSqlite())
                {
                    var conn = databaseFacade.GetDbConnection();
                    using (var command = conn.CreateCommand())
                    {
                        var opened = false;
                        if (conn.State != System.Data.ConnectionState.Open)
                        {
                            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                            opened = true;
                        }

                        if (databaseFacade.CurrentTransaction != null)
                            command.Transaction = databaseFacade.CurrentTransaction.GetDbTransaction();

                        try
                        {
                            command.CommandText = $@"SELECT CASE WHEN EXISTS (
                                SELECT * FROM ""sqlite_master"" WHERE ""type"" = 'table' AND ""rootpage"" IS NOT NULL AND ""tbl_name"" = '{tableName}'
                            )
                            THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT) END;";
                            var exists = (long)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));

                            return exists == 1;
                        }
                        finally
                        {
                            if (opened && conn.State == System.Data.ConnectionState.Open)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
                else
                {
                    var conn = databaseFacade.GetDbConnection();
                    using (var command = databaseFacade.GetDbConnection().CreateCommand())
                    {
                        var opened = false;
                        if (conn.State != System.Data.ConnectionState.Open)
                        {
                            await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                            opened = true;
                        }

                        if (databaseFacade.CurrentTransaction != null)
                            command.Transaction = databaseFacade.CurrentTransaction.GetDbTransaction();

                        try
                        {
                            command.CommandText = $@"SELECT CASE WHEN EXISTS (
                            SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME = '{tableName}'
                            )
                            THEN CAST(1 AS BIT)
                            ELSE CAST(0 AS BIT) END;";
                            var exists = (bool)(await command.ExecuteScalarAsync().ConfigureAwait(false));
                            return exists;
                        }
                        finally
                        {
                            if (opened && conn.State == System.Data.ConnectionState.Open)
                            {
                                conn.Close();
                            }
                        }
                    }
                }
            }
            else
            {
                return false;
            }
        }
#endregion

#region Create Tables Script
        public static string GenerateCreateTablesScript(this DbContext context)
        {
            var sql = context.Database.GenerateCreateScript();
            return sql;
        }
#endregion

#region Create Tables Commands
        public static List<string> GenerateCreateTablesCommands(this DbContext context)
        {
            var dependencies = context.Database.GetService<RelationalDatabaseCreatorDependencies>();

            IReadOnlyList<MigrationCommand> createTablesCommands = dependencies.MigrationsSqlGenerator.Generate(dependencies.ModelDiffer.GetDifferences(null, dependencies.Model.GetRelationalModel()), dependencies.Model);
            return createTablesCommands.Select(command => command.CommandText).ToList();
        }
#endregion

#region Migration Script
        public static string GenerateMigrationScript(this DatabaseFacade database, string fromMigration = null, string toMigration = null)
        {
            return database.GetService<IMigrator>().GenerateScript(fromMigration, toMigration);
        }
#endregion

#region Pending Migrations 
        public static IEnumerable<string> PendingMigrations(this DatabaseFacade database)
        {
            return database.GetPendingMigrations().ToList();
        }
#endregion

#region Migrations 
        public static IEnumerable<string> Migrations(this DatabaseFacade database)
        {
            return database.GetMigrations().ToList();
        }
#endregion

#region Apply Pending Migrations
        public static async Task EnsureMigratedStepByStepAsync(this DatabaseFacade database, CancellationToken cancellationToken = default(CancellationToken))
        {
            var pendingMigrations = PendingMigrations(database);
            if (pendingMigrations.Any())
            {
                var migrator = database.GetService<IMigrator>();
                foreach (var targetMigration in pendingMigrations)
                {
                    var sql = migrator.GenerateScript(targetMigration, targetMigration);
                    await migrator.MigrateAsync(targetMigration, cancellationToken).ConfigureAwait(false);
                }
            }
        }
#endregion

#region Get Entity Table Info
        public static (string TableName, string tableSchema, List<(string ColumnName, string ColumnType)> columns) EntityTableInfo<T>(this DbContext context)
        {
            return EntityTableInfo(context, typeof(T));
        }

        public static (string TableName, string tableSchema, List<(string ColumnName, string ColumnType)> columns) EntityTableInfo(this DbContext context, Type type)
        {
            var entityType = context.Model.FindEntityType(type);

            // Table info 

            var tableSchema = entityType.GetSchema();
            var tableName = entityType.GetTableName();

            //.NET Core 2.2
            //var tableName = entityType.Relational().TableName;
            //var tableSchema = entityType.Relational().Schema;

            var columns = new List<(string ColumnName, string ColumnType)>();

            // Column info 
            foreach (var property in entityType.GetProperties())
            {

                var columnName = property.GetColumnName();
                var columnType = property.GetColumnType();

            //.NET Core 2.2
            //var columnName = property.Relational().ColumnName;
            //var columnType = property.Relational().ColumnType;


                columns.Add((columnName, columnType));
            };

            return (tableName, tableSchema, columns);
        }
#endregion

#region Get Entity Model Types
        public static IEnumerable<IEntityType> GetEntityTypes(this DbContext context)
        {
            return context.Model.GetEntityTypes();
        }

        public static IEnumerable<Type> GetModelTypes(this DbContext context)
        {
            return context.Model.GetEntityTypes().Select(t => t.ClrType);
        }
#endregion
    }
}
