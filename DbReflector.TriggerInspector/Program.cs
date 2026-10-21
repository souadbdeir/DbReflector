using DbReflector;
using DbReflector.Common;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: dotnet run --project DbReflector.TriggerInspector -- \"<connection-string>\" <schema.table> <primary-key-column>");
    return 1;
}

var connectionString = args[0];
var applicationName = Environment.GetEnvironmentVariable("DBREFLECTOR_APPLICATION_NAME") ?? "trigger-inspector";
var requestedTable = args[1];
var primaryKeyColumn = args[2];

var schema = SchemaInfoFactory.CreateInstance(
    connectionString,
    CSharpTableInfoFormatter.CreateInstance(),
    applicationName,
    refreshMetadata: true)
    .Reflect();

var graph = schema.GetRelatedTablesRecursively(requestedTable, primaryKeyColumn);
if (graph.RootTable == null)
{
    Console.Error.WriteLine($"Could not find table '{requestedTable}' with column '{primaryKeyColumn}'.");
    return 1;
}

Console.WriteLine($"Root: {graph.RootTable.TableFQN}.{primaryKeyColumn}");
Console.WriteLine($"Tables found: {graph.Tables.Count}");
Console.WriteLine();

foreach (var table in graph.Tables)
{
    Console.WriteLine($"TABLE: {table.TableFQN}");
    Console.WriteLine("Columns:");
    foreach (var column in table.Columns.Values.OrderBy(column => column.OrdinalPosition))
    {
        Console.WriteLine($"  {column.OrdinalPosition}: {column.ColumnName} ({column.DataType}) " +
            $"PK={column.IsPrimaryKey} FK={column.IsForeignKey} Identity={column.IsIdentity} " +
            $"Computed={column.IsComputed} RowVersion={column.IsRowVersion} Default={column.HasDefaultConstraint}");
    }

    if (table.PrimaryKeyInfo != null)
        Console.WriteLine($"Primary key: {table.PrimaryKeyInfo.ConstraintName} ({string.Join(", ", table.PrimaryKeyInfo.Columns.Select(column => column.ColumnName))})");

    foreach (var uniqueConstraint in table.UniqueConstraints)
        Console.WriteLine($"Unique constraint: {uniqueConstraint.ConstraintName} ({string.Join(", ", uniqueConstraint.Columns.Select(column => column.ColumnName))})");

    foreach (var uniqueIndex in table.UniqueIndexes)
        Console.WriteLine($"Unique index: {uniqueIndex.IndexName} ({string.Join(", ", uniqueIndex.Columns.Select(column => column.ColumnName))})");

    foreach (var foreignKey in graph.ForeignKeys.Where(foreignKey => IsTable(foreignKey.ChildSchemaName, foreignKey.ChildTableName, table)))
        Console.WriteLine($"References: {foreignKey.ParentSchemaName}.{foreignKey.ParentTableName} ({FormatMappings(foreignKey)})");

    foreach (var foreignKey in graph.ForeignKeys.Where(foreignKey => IsTable(foreignKey.ParentSchemaName, foreignKey.ParentTableName, table)))
        Console.WriteLine($"Referenced by: {foreignKey.ChildSchemaName}.{foreignKey.ChildTableName} ({FormatMappings(foreignKey)})");

    foreach (var candidate in graph.RelationshipCandidates.Where(candidate => IsTable(candidate.ParentSchemaName, candidate.ParentTableName, table)))
        Console.WriteLine($"Possible referenced by: {candidate.ChildSchemaName}.{candidate.ChildTableName}.{candidate.ChildColumnName} -> {candidate.ParentColumnName} (name match only)");

    foreach (var trigger in table.Triggers)
    {
        Console.WriteLine($"Trigger: {trigger.TriggerName}");
        Console.WriteLine($"  Type: {trigger.TriggerType}");
        Console.WriteLine($"  Timing: {trigger.TriggerTiming}");
        Console.WriteLine($"  Enabled: {trigger.IsEnabled}");
        Console.WriteLine("  Definition:");
        Console.WriteLine(trigger.Definition);
    }

    Console.WriteLine();
}

Console.WriteLine($"Actual foreign keys found: {graph.ForeignKeys.Count}");
Console.WriteLine($"Inferred name-based candidates found: {graph.RelationshipCandidates.Count}");
return 0;

static bool IsTable(string schemaName, string tableName, ITableInfo table)
{
    return string.Equals(schemaName, table.Schema, StringComparison.InvariantCultureIgnoreCase)
        && string.Equals(tableName, table.TableName, StringComparison.InvariantCultureIgnoreCase);
}

static string FormatMappings(IForeignKeyInfo foreignKey)
{
    return string.Join(", ", foreignKey.Columns.Select(column =>
        $"{column.ChildColumnName}->{column.ParentColumnName}"));
}
