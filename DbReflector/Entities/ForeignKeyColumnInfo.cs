using DbReflector.Common;

namespace DbReflector;

public sealed class ForeignKeyColumnInfo : IForeignKeyColumnInfo
{
    public ForeignKeyColumnInfo(string childColumnName, string parentColumnName, int ordinalPosition)
    {
        ChildColumnName = childColumnName;
        ParentColumnName = parentColumnName;
        // Position in the composite fk
        OrdinalPosition = ordinalPosition;
    }

    public string ChildColumnName { get; }
    public string ParentColumnName { get; }
    public int OrdinalPosition { get; }
    public string DictionaryKey => $"{ChildColumnName}.{ParentColumnName}.{OrdinalPosition}";
}
