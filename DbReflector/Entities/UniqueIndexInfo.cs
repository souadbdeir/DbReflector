using DbReflector.Common;
using System.Collections.Generic;

namespace DbReflector;

public sealed class UniqueIndexInfo : IUniqueIndexInfo
{
    public UniqueIndexInfo(string schemaName, string tableName, string indexName, IReadOnlyList<IColumnPositionInfo> columns)
    {
        SchemaName = schemaName;
        TableName = tableName;
        IndexName = indexName;
        Columns = columns;
    }

    public string IndexName { get; }
    public string SchemaName { get; }
    public string TableName { get; }
    public IReadOnlyList<IColumnPositionInfo> Columns { get; }
    public string DictionaryKey => $"{SchemaName}.{TableName}.{IndexName}";
}
