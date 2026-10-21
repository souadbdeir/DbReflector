using System.Collections.Generic;

namespace DbReflector.Common;

public interface IUniqueConstraintInfo : ISchemaObjectInfo
{
    string ConstraintName { get; }
    string SchemaName { get; }
    string TableName { get; }
    IReadOnlyList<IColumnPositionInfo> Columns { get; }
}
