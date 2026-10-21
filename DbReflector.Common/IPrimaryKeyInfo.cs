using System.Collections.Generic;

namespace DbReflector.Common;

public interface IPrimaryKeyInfo : ISchemaObjectInfo
{
    string ConstraintName { get; }
    string SchemaName { get; }
    string TableName { get; }
    // Accoutn for composite keys
    IReadOnlyList<IColumnPositionInfo> Columns { get; }
}
