using System.Collections.Generic;

namespace DbReflector.Common;

public interface IRelationshipGraphInfo
{
    ITableInfo RootTable { get; }
    IReadOnlyList<ITableInfo> Tables { get; }
    IReadOnlyList<IForeignKeyInfo> ForeignKeys { get; }
    IReadOnlyList<IRelationshipCandidateInfo> RelationshipCandidates { get; }
}
