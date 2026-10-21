namespace DbReflector.Common;

public interface IForeignKeyColumnInfo : ISchemaObjectInfo
{
    string ChildColumnName { get; }
    string ParentColumnName { get; }
    int OrdinalPosition { get; }
}
