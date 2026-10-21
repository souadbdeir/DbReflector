using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Text;

namespace DbReflector;


public enum FunctionName
{
    ProcParameters,
    Procs,
    TableColumns,
    UdtColumns,
    TablesAndViews,
    Udts,
}

public class SqlHelper
{
    SQLiteConnection _SqliteConnection;
    SQLiteCommand _SqliteCommand;
    SQLiteDataAdapter _SqliteAdapter;

    SqlConnection _connection;
    SqlCommand _cmd;
    SqlDataAdapter _da;

    string _applicationName;
    string _connectionString;

    public static readonly string SqliteDb = "URI=file:" + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DbReflector.db"); //@"URI=file:C:\trash\dbReflector.db"
    SqlHelper(string connectionString, string applicationName, bool refreshMetaData)
    {
        _connectionString = connectionString;
        _applicationName = applicationName;

        _SqliteConnection = new SQLiteConnection(SqliteDb);
        _SqliteConnection.Open();
        _SqliteCommand = new SQLiteCommand();
        _SqliteCommand.Connection = _SqliteConnection;
        _SqliteAdapter = new SQLiteDataAdapter(_SqliteCommand);


        _connection = new SqlConnection(_connectionString);
        _connection.Open();
        _cmd = new SqlCommand();
        _cmd.Connection = _connection;
        _da = new SqlDataAdapter(_cmd);

        if (refreshMetaData)
        {
            //create the configuration tables in an Sqlite database
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_TableColumns_2));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_PrimaryKeys_15));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_UniqueConstraints_16));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_UniqueIndexes_17));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_ForeignKeys_21));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_Triggers_23));
            ExecuteSqliteNonQuery(TransformSql(Tables.CreateTable_TablesAndViews_4));
            ExecuteSqliteNonQuery(TransformSql(Procs.CreateTable_Procs_7));
            ExecuteSqliteNonQuery(TransformSql(Procs.CreateTable_ProcParams_9));
            ExecuteSqliteNonQuery(TransformSql(Procs.CreateTable_ProcColumns_10_1));
            ExecuteSqliteNonQuery(TransformSql(Udts.CreateTable_Udts_11));
            ExecuteSqliteNonQuery(TransformSql(Udts.CreateTable_UdtColumns_13));

            InsertIntoTableColumns(ExecuteDataTable(TransformSql(Tables.RefreshTable_TableColumns_3)));
            InsertIntoKeyMetadata(ExecuteDataTable(TransformSql(Tables.RefreshTable_PrimaryKeys_18)), "PrimaryKeys");
            InsertIntoKeyMetadata(ExecuteDataTable(TransformSql(Tables.RefreshTable_UniqueConstraints_19)), "UniqueConstraints");
            InsertIntoKeyMetadata(ExecuteDataTable(TransformSql(Tables.RefreshTable_UniqueIndexes_20)), "UniqueIndexes");
            InsertIntoForeignKeyMetadata(ExecuteDataTable(TransformSql(Tables.RefreshTable_ForeignKeys_22)));
            InsertIntoTriggerMetadata(ExecuteDataTable(TransformSql(Tables.RefreshTable_Triggers_24)));
            InsertIntoTablesAndViews(ExecuteDataTable(TransformSql(Tables.RefreshTable_TablesAndViews_5)));
            UpdateTablesAndViewsPrimaryKeysColumn(ExecuteSqliteDataTable(TransformSql(Tables.AddPrimaryKeysToTablesAndViews_6)));
            UpdateTablesAndViewsForeignKeysColumn(ExecuteSqliteDataTable(TransformSql(Tables.AddForeignKeysToTablesAndViews_6_1)));
            InsertIntoProcs(ExecuteDataTable(TransformSql(Procs.RefreshTable_Procs_8)));
            InsertIntoProcParams(ExecuteDataTable(TransformSql(Procs.RefreshTable_ProcParams_10)));
            InsertIntoProcColumns(ExecuteDataTable(TransformSql(Procs.RefreshTable_ProcColumns_10_2)));
            InsertIntoUdts(ExecuteDataTable(TransformSql(Udts.RefreshTable_Udts_12)));
            InsertIntoUdtColumns(ExecuteDataTable(TransformSql(Udts.RefreshTable_UdtColumns_14)));
        }

        EnsureTableColumnMetadataColumns();
        EnsureKeyMetadataTables();
    }

    void EnsureKeyMetadataTables()
    {
        ExecuteSqliteNonQuery(@"create table if not exists PrimaryKeys
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)");
        ExecuteSqliteNonQuery(@"create table if not exists UniqueConstraints
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)");
        ExecuteSqliteNonQuery(@"create table if not exists UniqueIndexes
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)");
        ExecuteSqliteNonQuery(@"create table if not exists ForeignKeys
(
    application_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    child_schema_name nvarchar(128) not null,
    child_table_name nvarchar(128) not null,
    parent_schema_name nvarchar(128) not null,
    parent_table_name nvarchar(128) not null,
    child_column_name nvarchar(128) not null,
    parent_column_name nvarchar(128) not null,
    ordinal_position int not null
)");
        ExecuteSqliteNonQuery(@"create table if not exists Triggers
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(128) not null,
    table_name nvarchar(128) not null,
    trigger_name nvarchar(128) not null,
    definition text,
    trigger_type nvarchar(50) not null,
    trigger_timing nvarchar(20) not null,
    is_enabled bit not null
)");
    }

    void EnsureTableColumnMetadataColumns()
    {
        foreach (var columnName in new[] { "IsIdentity", "IsComputed", "IsRowVersion", "HasDefaultConstraint" })
        {
            _SqliteCommand.CommandText = $"select count(*) from pragma_table_info('TableColumns') where name = '{columnName}'";
            var exists = Convert.ToInt32(_SqliteCommand.ExecuteScalar()) > 0;
            if (!exists)
            {
                _SqliteCommand.CommandText = $"alter table TableColumns add column {columnName} bit not null default(0)";
                _SqliteCommand.ExecuteNonQuery();
            }
        }
    }

    private void UpdateTablesAndViewsPrimaryKeysColumn(DataTable table)
    {
        var sb = new StringBuilder();
        foreach (DataRow row in table.Rows)
        {
            sb.Append($"update TablesAndViews set primary_keys='{row["Primary_Keys"]}' where application_name='{row["application_name"]}' and table_schema='{row["TABLE_SCHEMA"]}' and table_name ='{row["TABLE_NAME"]}';\n");
        }
        ExecuteSqliteNonQuery(sb.ToString());
    }
    private void UpdateTablesAndViewsForeignKeysColumn(DataTable table)
    {
        var sb = new StringBuilder();
        foreach (DataRow row in table.Rows)
        {
            sb.Append($"update TablesAndViews set foreign_keys='{row["foreign_keys"]}' where application_name='{row["application_name"]}' and TABLE_SCHEMA='{row["TABLE_SCHEMA"]}' and table_name ='{row["TABLE_NAME"]}';\n");
        }
        ExecuteSqliteNonQuery(sb.ToString());
    }

    public static SqlHelper CreateInstance(string connectionstring, string applicationName, bool refreshMetaData = false) => new SqlHelper(connectionstring, applicationName, refreshMetaData);

    public DataTable GetDataTable(string sql, string name)
    {
        DataTable table = new DataTable(name);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.CommandType = CommandType.Text;
        SQLiteDataAdapter da = new SQLiteDataAdapter(_SqliteCommand);

        da.Fill(table);
        return table;
    }
    void ExecuteSqliteNonQuery(string sql)
    {
        try
        {
            _SqliteCommand.CommandType = CommandType.Text;
            _SqliteCommand.CommandText = sql;
            _SqliteCommand.CommandTimeout = 60;
            _SqliteCommand.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    DataTable ExecuteSqliteDataTable(string sql)
    {
        try
        {
            _SqliteCommand.CommandType = CommandType.Text;
            _SqliteCommand.CommandText = sql;
            _SqliteCommand.CommandTimeout = 60;
            DataTable table = new DataTable();
            _SqliteAdapter.Fill(table);
            return table;
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    DataTable ExecuteDataTable(string sql)
    {
        try
        {
            _cmd.CommandType = CommandType.Text;
            _cmd.CommandText = sql;
            _cmd.CommandTimeout = 60;
            DataTable table = new DataTable();
            _da.Fill(table);
            return table;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    void InsertIntoUdtColumns(DataTable table)
    {
        if (table.Rows.Count == 0) return;
        var sql = $"INSERT INTO UdtColumns(application_name,schema_name,type_name,column_name,data_type,ordinal_number,max_length,precision,is_nullable,is_identity,is_computed) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["schema_name"]}','{row["type_name"]}','{row["column_name"]}','{row["data_type"]}','{row["ordinal_number"]}','{row["max_length"]}','{row["precision"]}','{row["is_nullable"]}','{row["is_identity"]}','{row["is_computed"]}')");
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }
    void InsertIntoProcColumns(DataTable table)
    {
        var sql = $"INSERT INTO ProcColumns(APPLICATION_NAME,SCHEMA_NAME,PROC_NAME,ORDINAL_POSITION,COLUMN_NAME,IsNullable,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,DOMAIN_NAME) VALUES";
        HashSet<string> set = new HashSet<string>();
        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            string key = $"{row["schema_name"]}-{row["proc_name"]}-{row["column_name"]}";
            if (!set.Contains(key))
            {
                values.Add($"('{row["application_name"]}','{row["schema_name"]}','{row["proc_name"]}','{row["ordinal_position"]}','{row["column_name"]}','{row["is_nullable"]}','{row["data_type"]}','{row["character_maximum_length"]}','{row["domain_name"]}')");
                set.Add(key);
            }
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }

    void InsertIntoProcParams(DataTable table)
    {
        var sql = $"INSERT INTO ProcParams (application_name,SchemaName,ProcName,ParameterName,OrdinalNumber,Direction,IsResult,ParameterType,ParameterMaxLen,ParameterNumericPrecision,ParameterDatePrecision,ParameterUDTName) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["SPECIFIC_SCHEMA"]}','{row["ROUTINE_NAME"]}','{row["PARAMETER_NAME"]}','{row["ORDINAL_POSITION"]}','{row["PARAMETER_MODE"]}','{row["IS_RESULT"]}','{row["DATA_TYPE"]}','{row["CHARACTER_MAXIMUM_LENGTH"]}','{row["NUMERIC_PRECISION"]}','{row["DATETIME_PRECISION"]}','{row["USER_DEFINED_TYPE_NAME"]}')");
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }
    void InsertIntoProcs(DataTable table)
    {
        var sql = $"INSERT INTO Procs (application_name,SchemaName,ProcName,IgnoreProc,SourceCode) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["SchemaName"]}','{row["ProcName"]}','{row["IgnoreProc"]}','{row["SourceCode"].ToString().Replace("'", "''")}')");
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }
    void InsertIntoUdts(DataTable table)
    {
        var sql = $"INSERT INTO Udts(application_name,schema_name,udt_name,ignore_udt) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["schema_name"]}','{row["udt_name"]}','{row["ignore_udt"]}')");
        }
        if (values.Count == 0) return;
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }
    void InsertIntoTableColumns(DataTable table)
    {
        var sql = $"INSERT INTO TableColumns (application_name,TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME,IsPrimaryKey,IsForeignKey,IsKey,IsNullable,IsIdentity,IsComputed,IsRowVersion,HasDefaultConstraint,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,ORDINAL_POSITION,NUMERIC_PRECISION,DOMAIN_NAME,IGNORE_COLUMN,COLUMN_ALIAS) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["TABLE_SCHEMA"]}','{row["TABLE_NAME"]}','{row["COLUMN_NAME"]}','{row["IsPrimaryKey"]}','{row["IsForeignKey"]}','{row["IsKey"]}','{row["IsNullable"]}','{row["IsIdentity"]}','{row["IsComputed"]}','{row["IsRowVersion"]}','{row["HasDefaultConstraint"]}','{row["DATA_TYPE"]}','{row["CHARACTER_MAXIMUM_LENGTH"]}','{row["ORDINAL_POSITION"]}','{row["NUMERIC_PRECISION"]}','{row["DOMAIN_NAME"]}','{row["IGNORE_COLUMN"]}','{row["COLUMN_ALIAS"]}')");
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }

    void InsertIntoKeyMetadata(DataTable table, string tableName)
    {
        if (table.Rows.Count == 0) return;

        var values = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            values.Add($"('{row["application_name"]}','{row["schema_name"]}','{row["table_name"]}','{row["constraint_name"]}','{row["column_name"]}','{row["ordinal_position"]}')");
        }

        _SqliteCommand.CommandText = $"insert into {tableName} (application_name,schema_name,table_name,constraint_name,column_name,ordinal_position) values {string.Join(",", values)}";
        _SqliteCommand.ExecuteNonQuery();
    }

    void InsertIntoForeignKeyMetadata(DataTable table)
    {
        if (table.Rows.Count == 0) return;

        var values = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            values.Add($"('{row["application_name"]}','{row["constraint_name"]}','{row["child_schema_name"]}','{row["child_table_name"]}','{row["parent_schema_name"]}','{row["parent_table_name"]}','{row["child_column_name"]}','{row["parent_column_name"]}','{row["ordinal_position"]}')");
        }

        _SqliteCommand.CommandText = $"insert into ForeignKeys (application_name,constraint_name,child_schema_name,child_table_name,parent_schema_name,parent_table_name,child_column_name,parent_column_name,ordinal_position) values {string.Join(",", values)}";
        _SqliteCommand.ExecuteNonQuery();
    }

    void InsertIntoTriggerMetadata(DataTable table)
    {
        if (table.Rows.Count == 0) return;

        var values = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            values.Add($"('{row["application_name"]}','{row["schema_name"]}','{row["table_name"]}','{row["trigger_name"]}','{row["definition"].ToString().Replace("'", "''")}','{row["trigger_type"]}','{row["trigger_timing"]}','{row["is_enabled"]}')");
        }

        _SqliteCommand.CommandText = $"insert into Triggers (application_name,schema_name,table_name,trigger_name,definition,trigger_type,trigger_timing,is_enabled) values {string.Join(",", values)}";
        _SqliteCommand.ExecuteNonQuery();
    }

    void InsertIntoTablesAndViews(DataTable table)
    {
        var sql = $"INSERT INTO TablesAndViews(application_name,table_schema,table_name,table_type,table_alias,IgnoreTable,is_enum) VALUES";

        var values = new List<string>();

        for (int row_index = 0; row_index < table.Rows.Count; row_index++)
        {
            DataRow row = table.Rows[row_index];
            values.Add($"('{row["application_name"]}','{row["table_schema"]}','{row["table_name"]}','{row["table_type"]}','{row["table_alias"]}','{row["IgnoreTable"]}','{row["is_enum"]}')");
        }
        sql += string.Join(",", values);
        _SqliteCommand.CommandText = sql;
        _SqliteCommand.ExecuteNonQuery();
    }

    #region Code Templates
    const string CreateSchema_DbReflector_1 = @"
IF not exists(SELECT * FROM sys.schemas WHERE name = N'DbReflector')
BEGIN	
	declare @sql nvarchar(4000) = 'create schema DbReflector';
    EXEC sp_executesql @sql
End
";

    public string TransformSql(string s) => s.Replace("<@applicationName@>", _applicationName).Replace("<@excludedSchemasList@>", "'DbReflector','bTbParams','sys'");

    static class Tables
    {
        public const string CreateTable_TableColumns_2 = @"
drop table if exists TableColumns;create table TableColumns
(
    application_name nvarchar(50) not null
    ,TABLE_SCHEMA nvarchar(50) not null
    ,TABLE_NAME nvarchar(50) not null
    ,COLUMN_NAME nvarchar(50) not null
    ,IsPrimaryKey bit not null
    ,IsForeignKey bit  not null
    ,IsKey bit not null
    ,IsNullable bit not null
    ,IsIdentity bit not null default(0)
    ,IsComputed bit not null default(0)
    ,IsRowVersion bit not null default(0)
    ,HasDefaultConstraint bit not null default(0)
    ,DATA_TYPE nvarchar(50) not null
    ,CHARACTER_MAXIMUM_LENGTH int
    ,ORDINAL_POSITION int not null
    ,NUMERIC_PRECISION tinyint
    ,DOMAIN_NAME nvarchar(50)
    ,IGNORE_COLUMN bit not null default(0)
    ,COLUMN_ALIAS nvarchar(50) not null
)
;create unique index u_bTb_tblcols on TableColumns (application_name, TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME)
;create unique index u_bTb_tblcols_colalias on TableColumns (application_name, TABLE_SCHEMA,TABLE_NAME,COLUMN_NAME)";
        public const string CreateTable_PrimaryKeys_15 = @"
drop table if exists PrimaryKeys;create table PrimaryKeys
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)
;create unique index u_bTb_primarykeys on PrimaryKeys(application_name,schema_name,table_name,constraint_name,column_name)";
        public const string CreateTable_UniqueConstraints_16 = @"
drop table if exists UniqueConstraints;create table UniqueConstraints
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)
;create unique index u_bTb_uniqueconstraints on UniqueConstraints(application_name,schema_name,table_name,constraint_name,column_name)";
        public const string CreateTable_UniqueIndexes_17 = @"
drop table if exists UniqueIndexes;create table UniqueIndexes
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    table_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    column_name nvarchar(128) not null,
    ordinal_position int not null
)
;create unique index u_bTb_uniqueindexes on UniqueIndexes(application_name,schema_name,table_name,constraint_name,column_name)";
        public const string RefreshTable_PrimaryKeys_18 = @"
declare @app varchar(50) = '<@applicationName@>';
select @app application_name, s.name schema_name, t.name table_name, i.name constraint_name, c.name column_name, ic.key_ordinal ordinal_position
from sys.tables t
inner join sys.schemas s on s.schema_id = t.schema_id
inner join sys.indexes i on i.object_id = t.object_id and i.is_primary_key = 1
inner join sys.index_columns ic on ic.object_id = i.object_id and ic.index_id = i.index_id and ic.key_ordinal > 0
inner join sys.columns c on c.object_id = ic.object_id and c.column_id = ic.column_id
where t.is_ms_shipped = 0 and s.name not in (<@excludedSchemasList@>)";
        public const string RefreshTable_UniqueConstraints_19 = @"
declare @app varchar(50) = '<@applicationName@>';
select @app application_name, s.name schema_name, t.name table_name, kc.name constraint_name, c.name column_name, ic.key_ordinal ordinal_position
from sys.key_constraints kc
inner join sys.tables t on t.object_id = kc.parent_object_id
inner join sys.schemas s on s.schema_id = t.schema_id
inner join sys.index_columns ic on ic.object_id = kc.parent_object_id and ic.index_id = kc.unique_index_id and ic.key_ordinal > 0
inner join sys.columns c on c.object_id = ic.object_id and c.column_id = ic.column_id
where kc.type = 'UQ' and t.is_ms_shipped = 0 and s.name not in (<@excludedSchemasList@>)";
        public const string RefreshTable_UniqueIndexes_20 = @"
declare @app varchar(50) = '<@applicationName@>';
select @app application_name, s.name schema_name, t.name table_name, i.name constraint_name, c.name column_name, ic.key_ordinal ordinal_position
from sys.indexes i
inner join sys.tables t on t.object_id = i.object_id
inner join sys.schemas s on s.schema_id = t.schema_id
inner join sys.index_columns ic on ic.object_id = i.object_id and ic.index_id = i.index_id and ic.key_ordinal > 0
inner join sys.columns c on c.object_id = ic.object_id and c.column_id = ic.column_id
where i.is_unique = 1 and i.is_primary_key = 0 and i.is_unique_constraint = 0 and i.is_hypothetical = 0
  and t.is_ms_shipped = 0 and s.name not in (<@excludedSchemasList@>)";
        public const string CreateTable_ForeignKeys_21 = @"
drop table if exists ForeignKeys;create table ForeignKeys
(
    application_name nvarchar(50) not null,
    constraint_name nvarchar(128) not null,
    child_schema_name nvarchar(128) not null,
    child_table_name nvarchar(128) not null,
    parent_schema_name nvarchar(128) not null,
    parent_table_name nvarchar(128) not null,
    child_column_name nvarchar(128) not null,
    parent_column_name nvarchar(128) not null,
    ordinal_position int not null
)
;create unique index u_bTb_foreignkeys on ForeignKeys(application_name,child_schema_name,child_table_name,constraint_name,ordinal_position)";
        public const string RefreshTable_ForeignKeys_22 = @"
declare @app varchar(50) = '<@applicationName@>';
select @app application_name,
       fk.name constraint_name,
       child_schema.name child_schema_name,
       child_table.name child_table_name,
       parent_schema.name parent_schema_name,
       parent_table.name parent_table_name,
       child_column.name child_column_name,
       parent_column.name parent_column_name,
       fkc.constraint_column_id ordinal_position
from sys.foreign_keys fk
inner join sys.tables child_table on child_table.object_id = fk.parent_object_id
inner join sys.schemas child_schema on child_schema.schema_id = child_table.schema_id
inner join sys.tables parent_table on parent_table.object_id = fk.referenced_object_id
inner join sys.schemas parent_schema on parent_schema.schema_id = parent_table.schema_id
inner join sys.foreign_key_columns fkc on fkc.constraint_object_id = fk.object_id
inner join sys.columns child_column on child_column.object_id = fkc.parent_object_id and child_column.column_id = fkc.parent_column_id
inner join sys.columns parent_column on parent_column.object_id = fkc.referenced_object_id and parent_column.column_id = fkc.referenced_column_id
where child_table.is_ms_shipped = 0
  and parent_table.is_ms_shipped = 0
  and child_schema.name not in (<@excludedSchemasList@>)
  and parent_schema.name not in (<@excludedSchemasList@>)";
        public const string CreateTable_Triggers_23 = @"
drop table if exists Triggers;create table Triggers
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(128) not null,
    table_name nvarchar(128) not null,
    trigger_name nvarchar(128) not null,
    definition text,
    trigger_type nvarchar(50) not null,
    trigger_timing nvarchar(20) not null,
    is_enabled bit not null
)
;create unique index u_bTb_triggers on Triggers(application_name,schema_name,table_name,trigger_name)";
        public const string RefreshTable_Triggers_24 = @"
declare @app varchar(50) = '<@applicationName@>';
select @app application_name,
       s.name schema_name,
       t.name table_name,
       tr.name trigger_name,
       sm.definition definition,
       tr.type_desc trigger_type,
       cast(case when tr.is_instead_of_trigger = 1 then 'INSTEAD OF' else 'AFTER' end as nvarchar(20)) trigger_timing,
       cast(case when tr.is_disabled = 1 then 0 else 1 end as bit) is_enabled
from sys.triggers tr
inner join sys.tables t on t.object_id = tr.parent_id
inner join sys.schemas s on s.schema_id = t.schema_id
left join sys.sql_modules sm on sm.object_id = tr.object_id
where tr.parent_class = 1
  and t.is_ms_shipped = 0
  and s.name not in (<@excludedSchemasList@>)";
        public const string RefreshTable_TableColumns_3 = @"
declare @app varchar(50) = '<@applicationName@>';
SET NOCOUNT ON
SET XACT_ABORT ON;

with source as
(
    select
        @app as application_name,
        s.name as TABLE_SCHEMA,
        o.name as TABLE_NAME,
        c.name as COLUMN_NAME,
        cast(case when exists
        (
            select 1
            from sys.indexes i
            inner join sys.index_columns ic on ic.object_id = i.object_id and ic.index_id = i.index_id
            where i.object_id = o.object_id
              and i.is_primary_key = 1
              and ic.column_id = c.column_id
        ) then 1 else 0 end as bit) as IsPrimaryKey,
        cast(case when exists
        (
            select 1
            from sys.foreign_key_columns fkc
            where fkc.parent_object_id = o.object_id
              and fkc.parent_column_id = c.column_id
        ) then 1 else 0 end as bit) as IsForeignKey,
        cast(case when exists
        (
            select 1
            from INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE u
            inner join INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                on tc.CONSTRAINT_NAME = u.CONSTRAINT_NAME
               and tc.TABLE_SCHEMA = u.TABLE_SCHEMA
               and tc.TABLE_NAME = u.TABLE_NAME
            where u.TABLE_SCHEMA = info.TABLE_SCHEMA
              and u.TABLE_NAME = info.TABLE_NAME
              and u.COLUMN_NAME = info.COLUMN_NAME
        ) then 1 else 0 end as bit) as IsKey,
        cast(case when info.IS_NULLABLE = 'YES' then 1 else 0 end as bit) as IsNullable,
        info.DATA_TYPE as DATA_TYPE,
        info.CHARACTER_MAXIMUM_LENGTH as CHARACTER_MAXIMUM_LENGTH,
        info.ORDINAL_POSITION as ORDINAL_POSITION,
        info.NUMERIC_PRECISION as NUMERIC_PRECISION,
        info.DOMAIN_NAME as DOMAIN_NAME,
        cast(case when ic.object_id is not null then 1 else 0 end as bit) as IsIdentity,
        cast(case when cc.object_id is not null then 1 else 0 end as bit) as IsComputed,
        cast(case when type_name(c.system_type_id) in ('timestamp', 'rowversion') then 1 else 0 end as bit) as IsRowVersion,
        cast(case when dc.object_id is not null then 1 else 0 end as bit) as HasDefaultConstraint,
        cast(0 as bit) as IGNORE_COLUMN,
        c.name as COLUMN_ALIAS
    from sys.objects o
    inner join sys.schemas s on s.schema_id = o.schema_id
    inner join sys.columns c on c.object_id = o.object_id
    inner join INFORMATION_SCHEMA.COLUMNS info on info.TABLE_SCHEMA = s.name and info.TABLE_NAME = o.name and info.COLUMN_NAME = c.name
    left join sys.types user_type on user_type.user_type_id = c.user_type_id
    left join sys.identity_columns ic on ic.object_id = c.object_id and ic.column_id = c.column_id
    left join sys.computed_columns cc on cc.object_id = c.object_id and cc.column_id = c.column_id
    left join sys.default_constraints dc on dc.parent_object_id = c.object_id and dc.parent_column_id = c.column_id
    where o.type in ('U', 'V')
      and o.is_ms_shipped = 0
      and o.name <> 'database_firewall_rules'
      and o.name <> 'sysdiagrams'
      and c.name not like 'graph_id_%'
      and c.name not like '$edge_id_%'
      and c.name not like 'to_obj_id_%'
      and c.name not like 'from_obj_id_%'
      and c.name not like '$to_obj_id_%'
      and c.name not like '$to_id_%'
      and c.name not like 'to_id_%'
      and c.name not like '$node_id_%'
      and c.name not like '$from_id_%'
      and s.name not in (<@excludedSchemasList@>)
)
select * from source
";
        public const string CreateTable_TablesAndViews_4 = @"
drop table if exists TablesAndViews;create table TablesAndViews
(
    application_name nvarchar(50) not null
	,table_schema nvarchar(50) not null
	,table_name nvarchar(50)   not null
	,table_type nvarchar(50)   not null
	,table_alias nvarchar(50)  not null
	,IgnoreTable bit           not null
    ,is_enum bit not null 
	,primary_keys nvarchar(2000)        
	,foreign_keys nvarchar(2000)        
)
;CREATE UNIQUE INDEX u_tblvws ON  TablesAndViews (application_name , table_schema,table_name)
;CREATE UNIQUE INDEX u_tblvws_alias ON  TablesAndViews (application_name ,table_schema,table_alias);
";
        public const string RefreshTable_TablesAndViews_5 = @"
declare @app varchar(50) = '<@applicationName@>'
SET NOCOUNT ON
SET XACT_ABORT ON;
SELECT @app application_name, table_schema,table_name,table_type,table_name table_alias, 0 IgnoreTable,0 is_enum,''primary_keys,''foreign_keys FROM INFORMATION_SCHEMA.tables where table_schema not in (<@excludedSchemasList@>)
";
        public const string AddPrimaryKeysToTablesAndViews_6 = @"
SELECT tc.application_name, tc.TABLE_SCHEMA, tc.TABLE_NAME, group_concat(tc.COLUMN_NAME) Primary_Keys FROM TableColumns tc
         WHERE tc.IsPrimaryKey = 'True'
         AND tc.application_name='<@applicationName@>'
         GROUP BY tc.application_name,tc.TABLE_SCHEMA, tc.TABLE_NAME
";
        public const string AddForeignKeysToTablesAndViews_6_1 = @"
SELECT tc.application_name, tc.TABLE_SCHEMA, tc.TABLE_NAME, group_concat(tc.COLUMN_NAME) foreign_keys FROM TableColumns tc
         WHERE tc.IsForeignKey = 'True'
         AND tc.application_name='<@applicationName@>'
         GROUP BY tc.application_name,tc.TABLE_SCHEMA, tc.TABLE_NAME
";
    }

    static class Procs
    {

        public const string CreateTable_Procs_7 = @"
drop table if exists Procs;create table Procs
(
    application_name nvarchar(50) not null
	,SchemaName nvarchar(50) not null
	,ProcName nvarchar(150) not null
	,IgnoreProc bit not null
    ,SourceCode text not null
)
;create unique index bTb_procs on Procs(application_name,SchemaName,ProcName)
";
        public const string RefreshTable_Procs_8 = @"
declare @app varchar(50) = '<@applicationName@>';
SET NOCOUNT ON;
SET XACT_ABORT ON;
WITH source
     AS (SELECT @app application_name, 
                r.SPECIFIC_SCHEMA SchemaName, 
                r.ROUTINE_NAME ProcName, 
                CAST(0 AS BIT) IgnoreProc,
                r.ROUTINE_DEFINITION SourceCode
         FROM information_schema.routines AS r
         WHERE routine_type = 'PROCEDURE'
               AND LEFT(Routine_Name, 3) NOT IN('sp_', 'xp_', 'ms_')
         AND r.ROUTINE_BODY = 'SQL'
         AND r.SPECIFIC_SCHEMA NOT IN(<@excludedSchemasList@>))
select * from source
";
        public const string CreateTable_ProcParams_9 = @"
drop table if exists ProcParams;create table ProcParams
( 
        application_name nvarchar(50) not null,
		SchemaName NVARCHAR(50) NOT NULL, 
		ProcName NVARCHAR(150) NOT NULL, 
		ParameterName NVARCHAR(150) NOT NULL, 
		OrdinalNumber INT NOT NULL, 
		Direction VARCHAR(10) NOT NULL, 
		IsResult BIT NOT NULL, 
		ParameterType NVARCHAR(50) NOT NULL, 
		ParameterMaxLen INT NULL, 
		ParameterNumericPrecision INT NULL, 
		ParameterDatePrecision INT NULL, 
		ParameterUDTName NVARCHAR(50) NULL
)
;create unique index bTb_procparam on ProcParams(application_name, SchemaName,ProcName,ParameterName)
";
        public const string RefreshTable_ProcParams_10 = @"
DECLARE @app VARCHAR(50)= '<@applicationName@>';
SET NOCOUNT ON
SET XACT_ABORT ON;
WITH source AS 
(
	SELECT @app application_name,
        r.SPECIFIC_SCHEMA, 
       r.ROUTINE_NAME, 
       p.PARAMETER_NAME, 
       p.ORDINAL_POSITION, 
       p.PARAMETER_MODE, 
       cast(IIF(p.IS_RESULT='NO',0,1) as bit) IS_RESULT, 
       p.DATA_TYPE, 
       p.CHARACTER_MAXIMUM_LENGTH, 
       p.NUMERIC_PRECISION, 
       p.DATETIME_PRECISION, 
       p.USER_DEFINED_TYPE_NAME 
FROM information_schema.routines AS r
     INNER JOIN information_schema.parameters AS p ON r.specific_schema = p.specific_schema
                                                      AND r.specific_name = p.specific_name
WHERE routine_type = 'PROCEDURE'
    AND LEFT(Routine_Name, 3) NOT IN('sp_', 'xp_', 'ms_')
    AND r.ROUTINE_BODY = 'SQL'
    AND r.SPECIFIC_SCHEMA not in (<@excludedSchemasList@>)
)
select * from source
";
        public const string CreateTable_ProcColumns_10_1 = @"
drop table if exists ProcColumns;create table ProcColumns
(
    APPLICATION_NAME nvarchar(50) not null,
	SCHEMA_NAME nvarchar(50) not null,
	PROC_NAME nvarchar(150) not null,
	ORDINAL_POSITION int not null,
	COLUMN_NAME nvarchar(150) not null,
	IsNullable bit not null,
	DATA_TYPE  nvarchar(50) not null,
	CHARACTER_MAXIMUM_LENGTH int,
    DOMAIN_NAME nvarchar(50),
    PRIMARY KEY (APPLICATION_NAME, SCHEMA_NAME, PROC_NAME, COLUMN_NAME)
)
";
        public const string RefreshTable_ProcColumns_10_2 = @"
declare @app varchar(50) = '<@applicationName@>'
SET NOCOUNT ON;
SET XACT_ABORT ON;
WITH source
     AS (SELECT @app application_name, s.name schema_name, 
                p.name proc_name, 
                column_ordinal ordinal_position, 
                isnull(r.name, 'id') column_name, 
                is_nullable, 
                system_type_name data_type, 
                max_length character_maximum_length, 
                user_type_name domain_name
         FROM sys.procedures AS p
              CROSS APPLY sys.dm_exec_describe_first_result_set_for_object(p.object_id, 0) AS r
              INNER JOIN sys.schemas AS s ON s.schema_id = p.schema_id
         WHERE is_hidden = 0 AND s.name NOT IN (<@excludedSchemasList@>))
select * from source
";
    }

    static class Udts
    {
        public const string CreateTable_Udts_11 = @"
drop table if exists Udts;create table Udts
(
    application_name nvarchar(50) not null,
	schema_name nvarchar(50) not null,
	udt_name nvarchar(150) not null,
	ignore_udt bit not null
)
;create unique index bTb_udts on udts(application_name, schema_name,udt_name)
";
        public const string RefreshTable_Udts_12 = @"
declare @app varchar(50) = '<@applicationName@>'
SET NOCOUNT ON
SET XACT_ABORT ON;

with source as
(
	select @app application_name, s.name schema_name, t.name udt_name,0 ignore_udt from sys.types t inner join sys.schemas s on t.schema_id=s.schema_id
	where is_user_defined = 1 and is_table_type=1 and s.name not in (<@excludedSchemasList@>)
)
select * from source
";
        public const string CreateTable_UdtColumns_13 = @"
drop table if exists UdtColumns;create table UdtColumns
(
    application_name nvarchar(50) not null,
    schema_name nvarchar(50) not null,
    type_name NVARCHAR(150) NOT NULL, 
    column_name NVARCHAR(150) NOT NULL, 
    data_type NVARCHAR(50) NOT NULL, 
    ordinal_number INT NOT NULL, 
    max_length INT , 
    precision int,
    is_nullable bit not null,
    is_identity bit not null, 
    is_computed bit not null
)
;create unique index bTb_udtcols on UdtColumns (application_name,schema_name,type_name,column_name)
";
        public const string RefreshTable_UdtColumns_14 = @"
declare @app varchar(50) = '<@applicationName@>'
SET NOCOUNT ON
SET XACT_ABORT ON;
with source as (
select 
	@app application_name
	,ss.name schema_name
	,t.name [type_name]
	,c.name [column_name]
	,y.name [data_type]
	,c.column_id ordinal_number
	,c.max_length
	,c.precision
	,c.is_nullable
	,c.is_identity
	,c.is_computed
from sys.table_types t
inner join sys.columns c on c.object_id = t.type_table_object_id
inner join sys.types y on y.user_type_id = c.user_type_id
inner join sys.schemas ss on ss.schema_id = t.schema_id
where t.is_user_defined = 1
  and t.is_table_type = 1
  and ss.name not in (<@excludedSchemasList@>)
  ) 
select * from source
";
    }
    #endregion
}
