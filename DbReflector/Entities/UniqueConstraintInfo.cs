using DbReflector.Common;
using System.Collections.Generic;

namespace DbReflector;

public sealed class UniqueConstraintInfo : IUniqueConstraintInfo
{
    public UniqueConstraintInfo(string schemaName, string tableName, string constraintName, IReadOnlyList<IColumnPositionInfo> columns)
    {
        SchemaName = schemaName;
        TableName = tableName;
        ConstraintName = constraintName;
        Columns = columns;
    }

    public string ConstraintName { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    public IReadOnlyList<IColumnPositionInfo> Columns { get; }
    public string DictionaryKey => $"{SchemaName}.{TableName}.{ConstraintName}";
}
