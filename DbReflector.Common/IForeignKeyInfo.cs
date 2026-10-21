using System.Collections.Generic;

namespace DbReflector.Common;

public interface IForeignKeyInfo : ISchemaObjectInfo
{
    string ConstraintName { get; }
    string ChildSchemaName { get; }
    string ChildTableName { get; }
    string ParentSchemaName { get; }
    string ParentTableName { get; }
    IReadOnlyList<IForeignKeyColumnInfo> Columns { get; }
}
