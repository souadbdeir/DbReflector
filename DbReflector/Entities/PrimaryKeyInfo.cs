using DbReflector.Common;
using System.Collections.Generic;

namespace DbReflector;

public sealed class PrimaryKeyInfo : IPrimaryKeyInfo
{
    public PrimaryKeyInfo(string schemaName, string tableName, string constraintName, IReadOnlyList<IColumnPositionInfo> columns)
    {
        SchemaName = schemaName;
        TableName = tableName;
        ConstraintName = constraintName;
        Columns = columns;
    }

    public string ConstraintName { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    // List to account for composite keys
    public IReadOnlyList<IColumnPositionInfo> Columns { get; }
    public string DictionaryKey => $"{SchemaName}.{TableName}.{ConstraintName}";
}
