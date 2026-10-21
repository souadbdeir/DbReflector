using DbReflector.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbReflector
{
    public class SchemaInfo : ISchemaInfo
    {
        public string ConnectionString { get; private set; }
        public SchemaInfo(string connectionString, ITableInfoFormatter formatter)
        {
            ConnectionString = connectionString;
            this.Formatter = formatter;
        }
        public Dictionary<string, IProcParamInfo> ProcParameters { get; set; }
        public Dictionary<string, IProcInfo> Procedures { get; set; }
        public Dictionary<string, IColumnInfo> TableColumns { get; set; }
        public Dictionary<string, IUdtColumnInfo> UdtColumns { get; set; }
        public Dictionary<string, ITableInfo> TablesAndViews { get; set; }
        public Dictionary<string, IUdtInfo> UDTs { get; set; }
        public string CacheFile { get; set; }
        #region Tables
        Dictionary<string, ITableInfo> _Tables;
        public Dictionary<string, ITableInfo> Tables
        {
            get
            {
                if (_Tables == null)
                {
                    _Tables = TablesAndViews.Where(p => p.Value.TableType == "BASE TABLE").ToDictionary(p => p.Key, p => p.Value, StringComparer.InvariantCultureIgnoreCase);
                }
                return _Tables;
            }
        }
        #endregion

        #region Views
        Dictionary<string, ITableInfo> _Views;
        public Dictionary<string, ITableInfo> Views
        {
            get
            {
                if (_Views == null)
                {
                    _Views = TablesAndViews.Where(p => p.Value.TableType == "VIEW").ToDictionary(p => p.Key, p => p.Value, StringComparer.InvariantCultureIgnoreCase);
                }
                return _Views;
            }
        }


        #endregion

        public ISchemaInfo Refresh()
        {
            Procedures = null;
            ProcParameters = null;
            TableColumns = null;
            TablesAndViews = null;
            _Tables = null;
            _Views = null;
            UdtColumns = null;
            UDTs = null;
            return this;
        }
        public ITableInfoFormatter Formatter { get; }

        public IReadOnlyList<IForeignKeyInfo> GetForeignKeysReferencing(string tableName, string primaryKeyColumnName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(primaryKeyColumnName) || TablesAndViews == null)
                return Array.Empty<IForeignKeyInfo>();

            return TablesAndViews.Values
                .SelectMany(table => table.ForeignKeys ?? Array.Empty<IForeignKeyInfo>())
                .Where(foreignKey => IsParentTable(foreignKey, tableName)
                    && foreignKey.Columns.Any(column => string.Equals(
                        column.ParentColumnName,
                        primaryKeyColumnName,
                        StringComparison.InvariantCultureIgnoreCase)))
                .ToList();
        }

        public IReadOnlyList<IRelationshipCandidateInfo> FindRelationshipCandidates(string tableName, string primaryKeyColumnName)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(primaryKeyColumnName) || TablesAndViews == null)
                return Array.Empty<IRelationshipCandidateInfo>();

            var parentTables = Tables.Values
                .Where(table => IsTable(table, tableName)
                    && table.Columns.ContainsKey(primaryKeyColumnName))
                .ToList();

            return parentTables
                .SelectMany(parentTable => Tables.Values
                    .Where(childTable => !IsSameTable(childTable, parentTable))
                    .SelectMany(childTable => childTable.Columns.Values
                        .Where(childColumn => HasExpectedColumnName(childColumn.ColumnName, parentTable.TableName, primaryKeyColumnName))
                        .Where(childColumn => !HasActualForeignKey(childTable, parentTable, childColumn.ColumnName, primaryKeyColumnName))
                        .Select(childColumn => (IRelationshipCandidateInfo)new RelationshipCandidateInfo(
                            childTable.Schema,
                            childTable.TableName,
                            childColumn.ColumnName,
                            parentTable.Schema,
                            parentTable.TableName,
                            primaryKeyColumnName))))
                .ToList();
        }

        public IRelationshipGraphInfo GetRelatedTablesRecursively(string tableName, string primaryKeyColumnName)
        {
            var emptyResult = new RelationshipGraphInfo(
                null,
                Array.Empty<ITableInfo>(),
                Array.Empty<IForeignKeyInfo>(),
                Array.Empty<IRelationshipCandidateInfo>());

            if (string.IsNullOrWhiteSpace(tableName)
                || string.IsNullOrWhiteSpace(primaryKeyColumnName)
                || TablesAndViews == null)
                return emptyResult;

            var rootTable = Tables.Values.FirstOrDefault(table => IsTable(table, tableName)
                && table.Columns.ContainsKey(primaryKeyColumnName));
            if (rootTable == null)
                return emptyResult;

            var relatedTables = new Dictionary<string, ITableInfo>(StringComparer.InvariantCultureIgnoreCase);
            var foreignKeys = new Dictionary<string, IForeignKeyInfo>(StringComparer.InvariantCultureIgnoreCase);
            var relationshipCandidates = new Dictionary<string, IRelationshipCandidateInfo>(StringComparer.InvariantCultureIgnoreCase);
            var pendingTables = new Queue<(ITableInfo Table, bool IsRoot)>();

            relatedTables[rootTable.DictionaryKey] = rootTable;
            pendingTables.Enqueue((rootTable, true));

            while (pendingTables.Count > 0)
            {
                var (currentTable, isRoot) = pendingTables.Dequeue();

                foreach (var foreignKey in currentTable.ForeignKeys ?? Array.Empty<IForeignKeyInfo>())
                {
                    AddForeignKey(foreignKey);
                    AddTable(foreignKey.ParentSchemaName, foreignKey.ParentTableName, false);
                }

                foreach (var childTable in Tables.Values)
                {
                    foreach (var foreignKey in childTable.ForeignKeys ?? Array.Empty<IForeignKeyInfo>())
                    {
                        if (!IsParentTable(foreignKey, currentTable.TableFQN))
                            continue;

                        if (isRoot && !foreignKey.Columns.Any(column => string.Equals(
                                column.ParentColumnName,
                                primaryKeyColumnName,
                                StringComparison.InvariantCultureIgnoreCase)))
                            continue;

                        AddForeignKey(foreignKey);
                        AddTable(foreignKey.ChildSchemaName, foreignKey.ChildTableName, false);
                    }
                }

                var columnsToCheck = isRoot
                    ? new[] { primaryKeyColumnName }
                    : currentTable.PrimaryKeyInfo?.Columns.Select(column => column.ColumnName).ToArray()
                        ?? Array.Empty<string>();

                foreach (var columnName in columnsToCheck)
                {
                    foreach (var candidate in FindRelationshipCandidates(currentTable.TableFQN, columnName))
                    {
                        relationshipCandidates[candidate.DictionaryKey] = candidate;
                        AddTable(candidate.ChildSchemaName, candidate.ChildTableName, false);
                    }
                }
            }

            return new RelationshipGraphInfo(
                rootTable,
                relatedTables.Values.ToList(),
                foreignKeys.Values.ToList(),
                relationshipCandidates.Values.ToList());

            void AddForeignKey(IForeignKeyInfo foreignKey)
            {
                foreignKeys[foreignKey.DictionaryKey] = foreignKey;
            }

            void AddTable(string schemaName, string relatedTableName, bool relatedTableIsRoot)
            {
                var tableKey = $"{schemaName}.{relatedTableName}";
                if (!Tables.TryGetValue(tableKey, out var relatedTable)
                    || relatedTables.ContainsKey(relatedTable.DictionaryKey))
                    return;

                relatedTables[relatedTable.DictionaryKey] = relatedTable;
                pendingTables.Enqueue((relatedTable, relatedTableIsRoot));
            }
        }

        static bool IsParentTable(IForeignKeyInfo foreignKey, string tableName)
        {
            return string.Equals(foreignKey.ParentTableName, tableName, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals($"{foreignKey.ParentSchemaName}.{foreignKey.ParentTableName}", tableName, StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsTable(ITableInfo table, string tableName)
        {
            return string.Equals(table.TableName, tableName, StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(table.TableFQN, tableName, StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsSameTable(ITableInfo first, ITableInfo second)
        {
            return string.Equals(first.Schema, second.Schema, StringComparison.InvariantCultureIgnoreCase)
                && string.Equals(first.TableName, second.TableName, StringComparison.InvariantCultureIgnoreCase);
        }

        static bool HasExpectedColumnName(string childColumnName, string parentTableName, string parentColumnName)
        {
            return string.Equals(childColumnName, $"{parentTableName}{parentColumnName}", StringComparison.InvariantCultureIgnoreCase)
                || string.Equals(childColumnName, $"{parentTableName}_{parentColumnName}", StringComparison.InvariantCultureIgnoreCase);
        }

        static bool HasActualForeignKey(ITableInfo childTable, ITableInfo parentTable, string childColumnName, string parentColumnName)
        {
            return (childTable.ForeignKeys ?? Array.Empty<IForeignKeyInfo>())
                .Where(foreignKey => string.Equals(foreignKey.ParentSchemaName, parentTable.Schema, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(foreignKey.ParentTableName, parentTable.TableName, StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(foreignKey => foreignKey.Columns)
                .Any(column => string.Equals(column.ChildColumnName, childColumnName, StringComparison.InvariantCultureIgnoreCase)
                    && string.Equals(column.ParentColumnName, parentColumnName, StringComparison.InvariantCultureIgnoreCase));
        }

        
    }
}
