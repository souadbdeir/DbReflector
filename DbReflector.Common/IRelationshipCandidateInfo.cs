namespace DbReflector.Common;

public interface IRelationshipCandidateInfo : ISchemaObjectInfo
{
    string ChildSchemaName { get; }
    string ChildTableName { get; }
    string ChildColumnName { get; }
    string ParentSchemaName { get; }
    string ParentTableName { get; }
    string ParentColumnName { get; }
    bool IsInferred { get; }
}
