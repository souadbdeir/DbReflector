using DbReflector.Common;
using DbReflector.Helpers;
using NUnit.Framework;
using System;
using System.Data;
using System.Linq;

namespace DbReflector.UnitTesting;

public class ForeignKeyMetadata
{
    [Test]
    public void ReflectorIncludesChildParentAndOrderedCompositeMappings()
    {
        var factory = new InMemoryFactory();
        var schema = new SchemaInfo("", CSharpTableInfoFormatter.CreateInstance());

        DbTableReflector.CreateInstance().Reflect(factory, schema);

        var foreignKey = schema.Tables["dbo.Child"].ForeignKeys.Single();
        Assert.That(foreignKey.ConstraintName, Is.EqualTo("FK_Child_Parent"));
        Assert.That(foreignKey.ChildSchemaName, Is.EqualTo("dbo"));
        Assert.That(foreignKey.ChildTableName, Is.EqualTo("Child"));
        Assert.That(foreignKey.ParentSchemaName, Is.EqualTo("dbo"));
        Assert.That(foreignKey.ParentTableName, Is.EqualTo("Parent"));
        Assert.That(foreignKey.Columns.Select(column => column.ChildColumnName), Is.EqualTo(new[] { "ParentTenantId", "ParentId" }));
        Assert.That(foreignKey.Columns.Select(column => column.ParentColumnName), Is.EqualTo(new[] { "TenantId", "Id" }));
    }

    private sealed class InMemoryFactory : BaseDataTableFactory
    {
        public InMemoryFactory() : base("")
        {
            var tables = new DataTable();
            foreach (var name in new[] { "table_schema", "table_name", "table_type", "table_alias", "primary_keys", "foreign_keys", "IgnoreTable" })
                tables.Columns.Add(name);
            tables.Rows.Add("dbo", "Child", "BASE TABLE", "Child", "", "ParentTenantId,ParentId", "False");
            tables.Rows.Add("dbo", "Parent", "BASE TABLE", "Parent", "", "", "False");

            var columns = new DataTable();
            foreach (var name in new[] { "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "IsPrimaryKey", "IsForeignKey", "IsKey", "IsNullable", "IsIdentity", "IsComputed", "IsRowVersion", "HasDefaultConstraint", "DATA_TYPE", "CHARACTER_MAXIMUM_LENGTH", "ORDINAL_POSITION", "NUMERIC_PRECISION", "DOMAIN_NAME", "IGNORE_COLUMN", "COLUMN_ALIAS" })
                columns.Columns.Add(name);
            AddColumn(columns, "Child", "ParentTenantId", "True", "1");
            AddColumn(columns, "Child", "ParentId", "True", "2");
            AddColumn(columns, "Parent", "TenantId", "True", "1");
            AddColumn(columns, "Parent", "Id", "True", "2");

            var emptyKeys = KeyTable();
            var foreignKeys = new DataTable();
            foreach (var name in new[] { "application_name", "constraint_name", "child_schema_name", "child_table_name", "parent_schema_name", "parent_table_name", "child_column_name", "parent_column_name", "ordinal_position" })
                foreignKeys.Columns.Add(name);
            foreignKeys.Rows.Add("app", "FK_Child_Parent", "dbo", "Child", "dbo", "Parent", "ParentId", "Id", "2");
            foreignKeys.Rows.Add("app", "FK_Child_Parent", "dbo", "Child", "dbo", "Parent", "ParentTenantId", "TenantId", "1");

            TablesAndViews = new TablesAndViewsDataSet(tables, columns, emptyKeys, emptyKeys.Copy(), emptyKeys.Copy(), foreignKeys);
        }

        static void AddColumn(DataTable table, string tableName, string columnName, string isPrimaryKey, string ordinal)
        {
            table.Rows.Add("dbo", tableName, columnName, isPrimaryKey, tableName == "Child" ? "True" : "False", isPrimaryKey, "False", "False", "False", "False", "False", "int", DBNull.Value, ordinal, DBNull.Value, DBNull.Value, "False", columnName);
        }

        static DataTable KeyTable()
        {
            var table = new DataTable();
            foreach (var name in new[] { "application_name", "schema_name", "table_name", "constraint_name", "column_name", "ordinal_position" })
                table.Columns.Add(name);
            return table;
        }

        public override TablesAndViewsDataSet TablesAndViews { get; }
        public override ProcsDataSet Procs => throw new NotSupportedException();
        public override UdtsDataSet Udts => throw new NotSupportedException();
    }
}
