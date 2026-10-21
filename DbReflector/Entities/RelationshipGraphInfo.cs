using DbReflector.Common;
using System.Collections.Generic;

namespace DbReflector;

public sealed class RelationshipGraphInfo : IRelationshipGraphInfo
{
    public RelationshipGraphInfo(
        ITableInfo rootTable,
        IReadOnlyList<ITableInfo> tables,
        IReadOnlyList<IForeignKeyInfo> foreignKeys,
        IReadOnlyList<IRelationshipCandidateInfo> relationshipCandidates)
    {
        RootTable = rootTable;
        Tables = tables;
        ForeignKeys = foreignKeys;
        RelationshipCandidates = relationshipCandidates;
    }

    public ITableInfo RootTable { get; }
    public IReadOnlyList<ITableInfo> Tables { get; }
    public IReadOnlyList<IForeignKeyInfo> ForeignKeys { get; }
    public IReadOnlyList<IRelationshipCandidateInfo> RelationshipCandidates { get; }
}
