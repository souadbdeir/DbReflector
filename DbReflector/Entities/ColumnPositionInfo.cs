using DbReflector.Common;

namespace DbReflector;

public sealed class ColumnPositionInfo : IColumnPositionInfo
{
    public ColumnPositionInfo(string columnName, int ordinalPosition)
    {
        ColumnName = columnName;
        OrdinalPosition = ordinalPosition;
    }

    public string ColumnName { get; }
    public int OrdinalPosition { get; }
    public string DictionaryKey => $"{ColumnName}.{OrdinalPosition}";
}
