using DbReflector.Common;
using System.Collections.Generic;

namespace DbReflector;

public sealed class ForeignKeyInfo : IForeignKeyInfo
{
    public ForeignKeyInfo(
        string constraintName,
        string childSchemaName,
        string childTableName,
        string parentSchemaName,
        string parentTableName,
        // Account for composite fk
        IReadOnlyList<IForeignKeyColumnInfo> columns)
    {
        ConstraintName = constraintName;
        ChildSchemaName = childSchemaName;
        ChildTableName = childTableName;
        ParentSchemaName = parentSchemaName;
        ParentTableName = parentTableName;
        Columns = columns;
    }

    public string ConstraintName { get; }
    public string ChildSchemaName { get; }
    public string ChildTableName { get; }
    public string ParentSchemaName { get; }
    public string ParentTableName { get; }
    public IReadOnlyList<IForeignKeyColumnInfo> Columns { get; }
    public string DictionaryKey => $"{ChildSchemaName}.{ChildTableName}.{ConstraintName}";
}
