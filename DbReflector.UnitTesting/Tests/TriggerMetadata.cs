using DbReflector.Common;
using DbReflector.Helpers;
using NUnit.Framework;
using System;
using System.Data;
using System.Linq;

namespace DbReflector.UnitTesting;

public class TriggerMetadata
{
    [Test]
    public void ReflectorIncludesTriggerDefinitionTypeTimingAndEnabledState()
    {
        var factory = new InMemoryFactory();
        var schema = new SchemaInfo("", CSharpTableInfoFormatter.CreateInstance());

        DbTableReflector.CreateInstance().Reflect(factory, schema);

        var triggers = schema.Tables["dbo.Example"].Triggers;
        Assert.That(triggers, Has.Count.EqualTo(2));

        var afterTrigger = triggers.Single(trigger => trigger.TriggerName == "TR_Example_After");
        Assert.That(afterTrigger.SchemaName, Is.EqualTo("dbo"));
        Assert.That(afterTrigger.TableName, Is.EqualTo("Example"));
        Assert.That(afterTrigger.Definition, Is.EqualTo("create trigger TR_Example_After on dbo.Example after insert as select 1"));
        Assert.That(afterTrigger.TriggerType, Is.EqualTo("SQL_TRIGGER"));
        Assert.That(afterTrigger.TriggerTiming, Is.EqualTo("AFTER"));
        Assert.That(afterTrigger.IsEnabled, Is.True);

        var insteadOfTrigger = triggers.Single(trigger => trigger.TriggerName == "TR_Example_InsteadOf");
        Assert.That(insteadOfTrigger.TriggerTiming, Is.EqualTo("INSTEAD OF"));
        Assert.That(insteadOfTrigger.IsEnabled, Is.False);
    }

    private sealed class InMemoryFactory : BaseDataTableFactory
    {
        public InMemoryFactory() : base("")
        {
            var tables = new DataTable();
            foreach (var name in new[] { "table_schema", "table_name", "table_type", "table_alias", "primary_keys", "foreign_keys", "IgnoreTable" })
                tables.Columns.Add(name);
            tables.Rows.Add("dbo", "Example", "BASE TABLE", "Example", "", "", "False");

            var columns = new DataTable();
            foreach (var name in new[] { "TABLE_SCHEMA", "TABLE_NAME", "COLUMN_NAME", "IsPrimaryKey", "IsForeignKey", "IsKey", "IsNullable", "IsIdentity", "IsComputed", "IsRowVersion", "HasDefaultConstraint", "DATA_TYPE", "CHARACTER_MAXIMUM_LENGTH", "ORDINAL_POSITION", "NUMERIC_PRECISION", "DOMAIN_NAME", "IGNORE_COLUMN", "COLUMN_ALIAS" })
                columns.Columns.Add(name);
            columns.Rows.Add("dbo", "Example", "Id", "False", "False", "False", "False", "False", "False", "False", "False", "int", DBNull.Value, "1", DBNull.Value, DBNull.Value, "False", "Id");

            var emptyKeys = KeyTable();
            var triggers = new DataTable();
            foreach (var name in new[] { "application_name", "schema_name", "table_name", "trigger_name", "definition", "trigger_type", "trigger_timing", "is_enabled" })
                triggers.Columns.Add(name);
            triggers.Rows.Add("app", "dbo", "Example", "TR_Example_After", "create trigger TR_Example_After on dbo.Example after insert as select 1", "SQL_TRIGGER", "AFTER", "True");
            triggers.Rows.Add("app", "dbo", "Example", "TR_Example_InsteadOf", "create trigger TR_Example_InsteadOf on dbo.Example instead of delete as select 1", "SQL_TRIGGER", "INSTEAD OF", "False");

            TablesAndViews = new TablesAndViewsDataSet(tables, columns, emptyKeys, emptyKeys.Copy(), emptyKeys.Copy(), CreateForeignKeysTable(), triggers);
        }

        static DataTable KeyTable()
        {
            var table = new DataTable();
            foreach (var name in new[] { "application_name", "schema_name", "table_name", "constraint_name", "column_name", "ordinal_position" })
                table.Columns.Add(name);
            return table;
        }

        static DataTable CreateForeignKeysTable()
        {
            var table = new DataTable();
            foreach (var name in new[] { "application_name", "constraint_name", "child_schema_name", "child_table_name", "parent_schema_name", "parent_table_name", "child_column_name", "parent_column_name", "ordinal_position" })
                table.Columns.Add(name);
            return table;
        }

        public override TablesAndViewsDataSet TablesAndViews { get; }
        public override ProcsDataSet Procs => throw new NotSupportedException();
        public override UdtsDataSet Udts => throw new NotSupportedException();
    }
}
