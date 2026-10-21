using DbReflector.Common;
using DbReflector.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DbReflector
{
    public class DbTableReflector : ITableReflector
    {
        DbTableReflector() { }
        public static DbTableReflector CreateInstance() => new DbTableReflector();
        public ISchemaInfo Reflect(BaseDataTableFactory factory, ISchemaInfo schemaInfo)
        {
            //DataTableFactoryFromDb DbFactory = factory as DataTableFactoryFromDb;

            // Get the Columns first.
            var TableColumns = new Dictionary<string, IColumnInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (DataRow row in factory.TablesAndViews.TableColumns.Rows)
            {
                string schema          = DataRowHelper.GetString(row,"TABLE_SCHEMA");
                string table           = DataRowHelper.GetString(row,"TABLE_NAME");
                string column          = DataRowHelper.GetString(row,"COLUMN_NAME");
                bool isPrimaryKey      = DataRowHelper.GetBool(row,"IsPrimaryKey");
                bool isForeignKey      = DataRowHelper.GetBool(row,"IsForeignKey");
                bool isKey             = DataRowHelper.GetBool(row,"IsKey");
                bool isNullable        = DataRowHelper.GetBool(row,"IsNullable");
                bool isIdentity        = DataRowHelper.GetBool(row,"IsIdentity");
                bool isComputed        = DataRowHelper.GetBool(row,"IsComputed");
                bool isRowVersion      = DataRowHelper.GetBool(row,"IsRowVersion");
                bool hasDefault        = DataRowHelper.GetBool(row,"HasDefaultConstraint");
                string dataType        = DataRowHelper.GetString(row,"DATA_TYPE");
                int? maxLength         = DataRowHelper.GetNullableInt32(row,"CHARACTER_MAXIMUM_LENGTH");
                byte? numericPrecision = DataRowHelper.GetByte(row,"NUMERIC_PRECISION");
                string domainName      = DataRowHelper.GetString(row,"DOMAIN_NAME");
                int ordinalPosition    = DataRowHelper.GetInt32(row,"ORDINAL_POSITION");

                var columnAlias     = DataRowHelper.GetString(row,"COLUMN_ALIAS");
                bool ignoreColumn    = DataRowHelper.GetBool(row,"IGNORE_COLUMN");
                if (ignoreColumn)
                    continue;

                var c = ColumnInfo.CreateIntance(
                                        schema: schema,
                                        table: table,
                                        column: column,
                                        isPrimaryKey: isPrimaryKey,
                                        isForeignKey: isForeignKey,
                                        isKey: isKey,
                                        isNullable: isNullable,
                                        dataType: dataType,
                                        maxLength: maxLength,
                                        numericPrecision: numericPrecision,
                                        domainName: domainName,
                                        ordinalPosition: ordinalPosition,
                                        isIdentity: isIdentity,
                                        isComputed: isComputed,
                                        isRowVersion: isRowVersion,
                                        hasDefaultConstraint: hasDefault);
                TableColumns.Add(c.DictionaryKey, c);
            }
            schemaInfo.TableColumns = TableColumns;

            // Maps each table name to a list containing its primary-key definition.
            var primaryKeyDefinitionsByTable = ReadColumnDefinitions(factory.TablesAndViews.PrimaryKeys,
                (schema, table, name, columns) => (IPrimaryKeyInfo)new PrimaryKeyInfo(schema, table, name, columns));
            // Maps each table name to its single primary-key definition.
            var primaryKeyByTable = primaryKeyDefinitionsByTable.ToDictionary(
                group => group.Key,
                group => group.Value.Single(),
                StringComparer.InvariantCultureIgnoreCase);
            var uniqueConstraints = ReadColumnDefinitions(factory.TablesAndViews.UniqueConstraints,
                (schema, table, name, columns) => (IUniqueConstraintInfo)new UniqueConstraintInfo(schema, table, name, columns));
            var uniqueIndexes = ReadColumnDefinitions(factory.TablesAndViews.UniqueIndexes,
                (schema, table, name, columns) => (IUniqueIndexInfo)new UniqueIndexInfo(schema, table, name, columns));
            var foreignKeys = ReadForeignKeys(factory.TablesAndViews.ForeignKeys);
            var triggers = ReadTriggers(factory.TablesAndViews.Triggers);
            
            // Get the Tables and Views
            var TablesAndViews = new Dictionary<string, ITableInfo>(StringComparer.InvariantCultureIgnoreCase);
            foreach (DataRow row in factory.TablesAndViews.TablesAndViews.Rows)
            {
                var schemaName      = DataRowHelper.GetString(row,"table_schema");
                var tableName       = DataRowHelper.GetString(row,"table_name");
                var tableType       = DataRowHelper.GetString(row,"table_type");
                var tableAlias      = DataRowHelper.GetString(row,"table_alias");

                string[] AssignedPrimaryKey = null;
                string pk = DataRowHelper.GetString(row,"primary_keys");
                if (!string.IsNullOrWhiteSpace(pk))
                {
                    AssignedPrimaryKey = pk.Split(',').Select(p => p.Trim()).ToArray();
                }

                string[] AssignedForeignKey= null;
                string fk = DataRowHelper.GetString(row,"foreign_keys");
                if (!string.IsNullOrWhiteSpace(fk))
                {
                    AssignedForeignKey = fk.Split(',').Select(p => p.Trim()).ToArray();
                }


                bool ignoreTable    = DataRowHelper.GetBool(row,"IgnoreTable");
                if (ignoreTable)
                    continue;

                var Columns = schemaInfo.TableColumns.Where(p => p.Value.SchemaName.Equals(schemaName) && p.Value.UdtName.Equals(tableName)).ToDictionary(q => q.Value.ColumnName, q => q.Value, StringComparer.InvariantCultureIgnoreCase);

                TableInfo tableMeta = TableInfo.CreateInstance(schemaName, tableName,tableType, tableAlias, AssignedPrimaryKey, AssignedForeignKey, schemaInfo.Formatter, Columns);

                tableMeta.Columns_PrimaryKeys         = tableMeta.Columns.Values.Where(p => p.IsPrimaryKey).ToList();
                tableMeta.Columns_NotPrimaryKeys      = tableMeta.Columns.Values.Where(p => p.IsPrimaryKey == false).ToList();
                tableMeta.Columns_ForeignKeys         = tableMeta.Columns.Values.Where(p => p.IsForeignKey).ToList();
                tableMeta.Columns_NotForeignKeys      = tableMeta.Columns.Values.Where(p => p.IsForeignKey == false).ToList();
                tableMeta.Columns_Keys                = tableMeta.Columns.Values.Where(p => p.IsKey).ToList();
                tableMeta.Columns_NotKeys             = tableMeta.Columns.Values.Where(p => p.IsKey == false).ToList();
                tableMeta.Columns_Nullable            = tableMeta.Columns.Values.Where(p => p.IsNullable).ToList();
                tableMeta.Columns_Nullable_NotKeys    = tableMeta.Columns.Values.Where(p => p.IsNullable && p.IsKey == false).ToList();
                tableMeta.Columns_NotNullable         = tableMeta.Columns.Values.Where(p => p.IsNullable == false).ToList();
                tableMeta.Columns_NotNullable_NotKeys = tableMeta.Columns.Values.Where(p => p.IsNullable == false && p.IsKey == false).ToList();
                tableMeta.Columns_Audit               = tableMeta.Columns.Values.Where(p => p.IsAudit).ToList();
                tableMeta.Columns_InOrder             = new List<IColumnInfo>(tableMeta.Columns_PrimaryKeys).Concat(tableMeta.Columns_ForeignKeys).Concat(tableMeta.Columns_NotKeys).ToList();
                tableMeta.ModifiedById_Column         = tableMeta.Columns.SingleOrDefault(p => p.Value.IsModifiedByUserId).Value;
                tableMeta.CreatedById_Column          = tableMeta.Columns.SingleOrDefault(p => p.Value.IsCreatedByUserId).Value;
                tableMeta.CreatedDate_Column          = tableMeta.Columns.SingleOrDefault(p => p.Value.IsCreatedDate).Value;
                tableMeta.ModifiedDate_Column         = tableMeta.Columns.SingleOrDefault(p => p.Value.IsModifiedDate).Value;
                tableMeta.PrimaryKeyInfo               = primaryKeyByTable.TryGetValue(tableMeta.DictionaryKey, out var primaryKey) ? primaryKey : null;
                tableMeta.UniqueConstraints            = uniqueConstraints.TryGetValue(tableMeta.DictionaryKey, out var constraints) ? constraints : new List<IUniqueConstraintInfo>();
                tableMeta.UniqueIndexes                = uniqueIndexes.TryGetValue(tableMeta.DictionaryKey, out var indexes) ? indexes : new List<IUniqueIndexInfo>();
                tableMeta.ForeignKeys                  = foreignKeys.TryGetValue(tableMeta.DictionaryKey, out var relationships) ? relationships : new List<IForeignKeyInfo>();
                tableMeta.Triggers                      = triggers.TryGetValue(tableMeta.DictionaryKey, out var tableTriggers) ? tableTriggers : new List<ITriggerInfo>();

                if (!TablesAndViews.ContainsKey(tableMeta.DictionaryKey)) //in case the table has a compound primary key then add it only once
                    TablesAndViews.Add(tableMeta.DictionaryKey, tableMeta);

                //if (!Schemas.ContainsKey(schemaName))
                //{
                //    Schemas.Add(schemaName, new Dictionary<string, ITableInfo>(StringComparer.InvariantCultureIgnoreCase));
                //}
                //Schemas[schemaName].Add(tableMeta.DictionaryKey, tableMeta);
            }
            schemaInfo.TablesAndViews = TablesAndViews;

            return schemaInfo;
        }

        // Returns: "schema.table": a list of definitions, each with a name and ordered columns.
        // Example: "dbo.Customer": PK_Customer(CustomerId), UQ_Customer_Email(Email).
        static Dictionary<string, List<T>> ReadColumnDefinitions<T>(
            DataTable table,
            Func<string, string, string, IReadOnlyList<IColumnPositionInfo>, T> create)
        {
            var result = new Dictionary<string, List<T>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var tableGroup in KeyGroups(table).GroupBy(
                row => $"{DataRowHelper.GetString(row.First(), "schema_name")}.{DataRowHelper.GetString(row.First(), "table_name")}",
                StringComparer.InvariantCultureIgnoreCase))
            {
                result[tableGroup.Key] = tableGroup.Select(definitionGroup =>
                {
                    var first = definitionGroup.First();
                    return create(
                        DataRowHelper.GetString(first, "schema_name"),
                        DataRowHelper.GetString(first, "table_name"),
                        DataRowHelper.GetString(first, "constraint_name"),
                        ReadOrderedColumns(definitionGroup));
                }).ToList();
            }
            return result;
        }

        // Reads the ForeignKeys DataTable and returns each child table's foreign keys with child-to-parent column mappings.
        static Dictionary<string, List<IForeignKeyInfo>> ReadForeignKeys(DataTable table)
        {
            return table.Rows.Cast<DataRow>()
                .GroupBy(row =>
                    $"{DataRowHelper.GetString(row, "child_schema_name")}.{DataRowHelper.GetString(row, "child_table_name")}",
                    StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(
                    tableGroup => tableGroup.Key,
                    tableGroup => tableGroup
                        .GroupBy(row => DataRowHelper.GetString(row, "constraint_name"), StringComparer.InvariantCultureIgnoreCase)
                        .Select(group =>
                        {
                            var first = group.First();
                            return (IForeignKeyInfo)new ForeignKeyInfo(
                                DataRowHelper.GetString(first, "constraint_name"),
                                DataRowHelper.GetString(first, "child_schema_name"),
                                DataRowHelper.GetString(first, "child_table_name"),
                                DataRowHelper.GetString(first, "parent_schema_name"),
                                DataRowHelper.GetString(first, "parent_table_name"),
                                group.OrderBy(row => DataRowHelper.GetInt32(row, "ordinal_position"))
                                    .Select(row => (IForeignKeyColumnInfo)new ForeignKeyColumnInfo(
                                        DataRowHelper.GetString(row, "child_column_name"),
                                        DataRowHelper.GetString(row, "parent_column_name"),
                                        DataRowHelper.GetInt32(row, "ordinal_position")))
                                    .ToList());
                        })
                        .ToList(),
                    StringComparer.InvariantCultureIgnoreCase);
        }

        // Reads trigger rows and groups them by the table they belong to.
        static Dictionary<string, List<ITriggerInfo>> ReadTriggers(DataTable table)
        {
            return table.Rows.Cast<DataRow>()
                .GroupBy(row =>
                    $"{DataRowHelper.GetString(row, "schema_name")}.{DataRowHelper.GetString(row, "table_name")}",
                    StringComparer.InvariantCultureIgnoreCase)
                .ToDictionary(
                    tableGroup => tableGroup.Key,
                    tableGroup => tableGroup
                        .Select(row => (ITriggerInfo)new TriggerInfo(
                            DataRowHelper.GetString(row, "trigger_name"),
                            DataRowHelper.GetString(row, "schema_name"),
                            DataRowHelper.GetString(row, "table_name"),
                            DataRowHelper.GetString(row, "definition"),
                            DataRowHelper.GetString(row, "trigger_type"),
                            DataRowHelper.GetString(row, "trigger_timing"),
                            DataRowHelper.GetBool(row, "is_enabled")))
                        .ToList(),
                    StringComparer.InvariantCultureIgnoreCase);
        }

        // Groups primary-key and unique-definition rows by schema, table, and object name.
        static IEnumerable<IGrouping<string, DataRow>> KeyGroups(DataTable table)
        {
            return table.Rows.Cast<DataRow>().GroupBy(row =>
                $"{DataRowHelper.GetString(row, "schema_name")}.{DataRowHelper.GetString(row, "table_name")}.{DataRowHelper.GetString(row, "constraint_name")}",
                StringComparer.InvariantCultureIgnoreCase);
        }

        // Sorts columns by their position within the primary key or unique definition.
        static IReadOnlyList<IColumnPositionInfo> ReadOrderedColumns(IEnumerable<DataRow> rows)
        {
            return rows
                .OrderBy(row => DataRowHelper.GetInt32(row, "ordinal_position"))
                .Select(row => (IColumnPositionInfo)new ColumnPositionInfo(
                    DataRowHelper.GetString(row, "column_name"),
                    DataRowHelper.GetInt32(row, "ordinal_position")))
                .ToList();
        }

    }
}
