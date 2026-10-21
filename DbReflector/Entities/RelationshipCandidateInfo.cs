using DbReflector.Common;

namespace DbReflector;

public sealed class RelationshipCandidateInfo : IRelationshipCandidateInfo
{
    public RelationshipCandidateInfo(
        string childSchemaName,
        string childTableName,
        string childColumnName,
        string parentSchemaName,
        string parentTableName,
        string parentColumnName)
    {
        ChildSchemaName = childSchemaName;
        ChildTableName = childTableName;
        ChildColumnName = childColumnName;
        ParentSchemaName = parentSchemaName;
        ParentTableName = parentTableName;
        ParentColumnName = parentColumnName;
    }

    public string ChildSchemaName { get; }
    public string ChildTableName { get; }
    public string ChildColumnName { get; }
    public string ParentSchemaName { get; }
    public string ParentTableName { get; }
    public string ParentColumnName { get; }
    public bool IsInferred => true;
    public string DictionaryKey => $"{ChildSchemaName}.{ChildTableName}.{ChildColumnName}->{ParentSchemaName}.{ParentTableName}.{ParentColumnName}";
}
