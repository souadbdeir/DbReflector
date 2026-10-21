namespace DbReflector.Common;

/// <summary>
/// Identifies a column and its position within a primary key, unique constraint, or unique index.
/// </summary>
public interface IColumnPositionInfo : ISchemaObjectInfo
{
    string ColumnName { get; }
    int OrdinalPosition { get; }
}
