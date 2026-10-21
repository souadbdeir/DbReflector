using System.Collections.Generic;

namespace DbReflector.Common;

public interface IUniqueIndexInfo : ISchemaObjectInfo
{
    string IndexName { get; }
    string SchemaName { get; }
    string TableName { get; }
    IReadOnlyList<IColumnPositionInfo> Columns { get; }
}
