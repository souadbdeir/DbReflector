using System.Collections.Generic;

namespace DbReflector.Common;

public interface ITableInfo : ISchemaObjectInfo
{
    #region Columns
    Dictionary<string, IColumnInfo> Columns { get; }
    IReadOnlyList<IColumnInfo> Columns_Audit { get; }
    IReadOnlyList<IColumnInfo> Columns_ForeignKeys { get; }
    IReadOnlyList<IColumnInfo> Columns_InOrder { get; }
    IReadOnlyList<IColumnInfo> Columns_Keys { get; }
    IReadOnlyList<IColumnInfo> Columns_NotForeignKeys { get; }
    IReadOnlyList<IColumnInfo> Columns_NotKeys { get; }
    IReadOnlyList<IColumnInfo> Columns_NotNullable { get; }
    IReadOnlyList<IColumnInfo> Columns_NotNullable_NotKeys { get; }
    IReadOnlyList<IColumnInfo> Columns_NotPrimaryKeys { get; }
    IReadOnlyList<IColumnInfo> Columns_Nullable { get; }
    IReadOnlyList<IColumnInfo> Columns_Nullable_NotKeys { get; }
    /// <summary>
    /// The table columns participating in the primary key.
    /// </summary>
    IReadOnlyList<IColumnInfo> Columns_PrimaryKeys { get; }
    IColumnInfo CreatedById_Column { get; }
    IColumnInfo CreatedDate_Column { get; } 
    IColumnInfo ModifiedById_Column { get; }
    IColumnInfo ModifiedDate_Column { get; }
    
    /// <summary>
    /// Returns the first primary-key column for compatibility with the legacy API.
    /// </summary>
    IColumnInfo PrimaryKey { get; }

    /// <summary>
    /// The complete primary-key definition, including its name and ordered columns.
    /// </summary>
    IPrimaryKeyInfo PrimaryKeyInfo { get; }

    /// <summary>
    /// Unique constraints defined on the table.
    /// </summary>
    IReadOnlyList<IUniqueConstraintInfo> UniqueConstraints { get; }

    /// <summary>
    /// Unique indexes defined on the table.
    /// </summary>
    IReadOnlyList<IUniqueIndexInfo> UniqueIndexes { get; }

    /// <summary>
    /// Foreign-key relationships where this table is the child table.
    /// </summary>
    IReadOnlyList<IForeignKeyInfo> ForeignKeys { get; }
    #endregion
    
    string NewKeyId { get; }
    string NewKeyIdCsType { get; }
    string Schema { get; }
    
    #region Table
    string TableFQN { get; }
    string TableName { get; }
    string TableFQNIfNotDbo { get; }
    string TableType { get; } 
    string TableAlias{ get; } 
    
    /// <summary>
    /// Returns the primary key(s) set through configuration tables (used on views where a primary key is not available).
    /// </summary>
    string[] AssignedPrimaryKeys { get; }
    string[] AssignedForeignKeys { get; }
    #endregion

    ITableInfoFormatterDecorator Formatter { get; }
}
