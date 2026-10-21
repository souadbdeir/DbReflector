using System.Collections.Generic;

namespace DbReflector.Common;

public interface ISchemaInfo
{
    Dictionary<string, IProcInfo> Procedures { get; set; }
    Dictionary<string, IProcParamInfo> ProcParameters { get; set; }
    Dictionary<string, IColumnInfo> TableColumns { get; set; }
    Dictionary<string, ITableInfo> Tables { get; }
    Dictionary<string, ITableInfo> TablesAndViews { get; set; }
    Dictionary<string, IUdtColumnInfo> UdtColumns { get; set; }
    Dictionary<string, IUdtInfo> UDTs { get; set; }
    Dictionary<string, ITableInfo> Views { get; }
    string CacheFile { get; set; }
    ITableInfoFormatter Formatter { get; }
    IReadOnlyList<IForeignKeyInfo> GetForeignKeysReferencing(string tableName, string primaryKeyColumnName);
    // Finds possible child columns by matching the parent table and primary-key names; this is not necessarily a foreign-key relationship.
    IReadOnlyList<IRelationshipCandidateInfo> FindRelationshipCandidates(string tableName, string primaryKeyColumnName);
    // Starting from a table and primary-key column, recursively returns connected tables in both directions.
    IRelationshipGraphInfo GetRelatedTablesRecursively(string tableName, string primaryKeyColumnName);
    ISchemaInfo Refresh();
}
