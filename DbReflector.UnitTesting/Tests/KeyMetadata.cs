using DbReflector.Common;
using DbReflector.Helpers;
using NUnit.Framework;
using System;
using System.Data;
using System.Linq;

namespace DbReflector.UnitTesting;

public class KeyMetadata
{
    [Test]
    public void ReflectorSeparatesKeysAndPreservesColumnOrder()
    {
        var factory = new InMemoryFactory();
        var schema = new SchemaInfo("", CSharpTableInfoFormatter.CreateInstance());

        DbTableReflector.CreateInstance().Reflect(factory, schema);

        var table = schema.Tables["dbo.Example"];
        Assert.That(table.PrimaryKeyInfo.ConstraintName, Is.EqualTo("PK_Example"));
        Assert.That(table.PrimaryKeyInfo.Columns.Select(column => column.ColumnName), Is.EqualTo(new[] { "TenantId", "Id" }));
        Assert.That(table.UniqueConstraints.Select(key => key.ConstraintName), Is.EqualTo(new[] { "UQ_Example_Code" }));
        Assert.That(table.UniqueConstraints[0].Columns[0].ColumnName, Is.EqualTo("Code"));
        Assert.That(table.UniqueIndexes.Select(key => key.IndexName), Is.EqualTo(new[] { "UX_Example_Name" }));
        Assert.That(table.UniqueIndexes[0].Columns[0].ColumnName, Is.EqualTo("Name"));
    }

    private sealed class InMemoryFactory : BaseDataTableFactory
    {
        public InMemoryFactory() : base("")
        {
            var tables = new DataTable();
            tables.Columns.Add("table_schema");
            tables.Columns.Add("table_name");
            tables.Columns.Add("table_type");
            tables.Columns.Add("table_alias");
            tables.Columns.Add("primary_keys");
            tables.Columns.Add("foreign_keys");
            tables.Columns.Add("IgnoreTable");
            tables.Rows.Add("dbo", "Example", "BASE TABLE", "Example", "TenantId,Id", "", "False");

            var columns = new DataTable();
            foreach (var name in new[] { "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "IsPrimaryKey", "IsForeignKey", "IsKey", "IsNullable", "IsIdentity", "IsComputed", "IsRowVersion", "HasDefaultConstraint", "DATA_TYPE", "CHARACTER_MAXIMUM_LENGTH", "ORDINAL_POSITION", "NUMERIC_PRECISION", "DOMAIN_NAME", "IGNORE_COLUMN", "COLUMN_ALIAS" })
                columns.Columns.Add(name);
            columns.Rows.Add("dbo", "Example", "TenantId", "True", "False", "True", "False", "False", "False", "False", "False", "int", DBNull.Value, "1", DBNull.Value, DBNull.Value, "False", "TenantId");
            columns.Rows.Add("dbo", "Example", "Id", "True", "False", "True", "False", "True", "False", "False", "False", "int", DBNull.Value, "2", DBNull.Value, DBNull.Value, "False", "Id");
            columns.Rows.Add("dbo", "Example", "Code", "False", "False", "True", "False", "False", "False", "False", "False", "varchar", "20", "3", DBNull.Value, DBNull.Value, "False", "Code");
            columns.Rows.Add("dbo", "Example", "Name", "False", "False", "True", "False", "False", "False", "False", "False", "varchar", "50", "4", DBNull.Value, DBNull.Value, "False", "Name");

            var primaryKeys = KeyTable();
            primaryKeys.Rows.Add("app", "dbo", "Example", "PK_Example", "Id", "2");
            primaryKeys.Rows.Add("app", "dbo", "Example", "PK_Example", "TenantId", "1");

            var uniqueConstraints = KeyTable();
            uniqueConstraints.Rows.Add("app", "dbo", "Example", "UQ_Example_Code", "Code", "1");

            var uniqueIndexes = KeyTable();
            uniqueIndexes.Rows.Add("app", "dbo", "Example", "UX_Example_Name", "Name", "1");

            TablesAndViews = new TablesAndViewsDataSet(tables, columns, primaryKeys, uniqueConstraints, uniqueIndexes);
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
