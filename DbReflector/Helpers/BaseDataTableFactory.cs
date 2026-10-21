using System.Data;

namespace DbReflector.Helpers
{
    public abstract class BaseDataTableFactory
    {
        string connectionString;

        public class TablesAndViewsDataSet : DataSet
        {
            const string TABLES_AND_VIEWS = "TablesAndViews";
            const string TABLE_COLUMNS = "TableColumns";
            const string PRIMARY_KEYS = "PrimaryKeys";
            const string UNIQUE_CONSTRAINTS = "UniqueConstraints";
            const string UNIQUE_INDEXES = "UniqueIndexes";
            const string FOREIGN_KEYS = "ForeignKeys";
            const string TRIGGERS = "Triggers";

            public TablesAndViewsDataSet(DataTable tablesAndViews, DataTable tableColumns, DataTable primaryKeys, DataTable uniqueConstraints, DataTable uniqueIndexes)
                : this(tablesAndViews, tableColumns, primaryKeys, uniqueConstraints, uniqueIndexes, CreateForeignKeysTable(), CreateTriggersTable())
            {
            }

            public TablesAndViewsDataSet(DataTable tablesAndViews, DataTable tableColumns, DataTable primaryKeys, DataTable uniqueConstraints, DataTable uniqueIndexes, DataTable foreignKeys)
                : this(tablesAndViews, tableColumns, primaryKeys, uniqueConstraints, uniqueIndexes, foreignKeys, CreateTriggersTable())
            {
            }

            public TablesAndViewsDataSet(DataTable tablesAndViews, DataTable tableColumns, DataTable primaryKeys, DataTable uniqueConstraints, DataTable uniqueIndexes, DataTable foreignKeys, DataTable triggers)
            {
                tablesAndViews.TableName = TABLES_AND_VIEWS;
                Tables.Add(tablesAndViews);

                tableColumns.TableName = TABLE_COLUMNS;
                Tables.Add(tableColumns);

                primaryKeys.TableName = PRIMARY_KEYS;
                Tables.Add(primaryKeys);

                uniqueConstraints.TableName = UNIQUE_CONSTRAINTS;
                Tables.Add(uniqueConstraints);

                uniqueIndexes.TableName = UNIQUE_INDEXES;
                Tables.Add(uniqueIndexes);

                foreignKeys.TableName = FOREIGN_KEYS;
                Tables.Add(foreignKeys);

                triggers.TableName = TRIGGERS;
                Tables.Add(triggers);
            }

            static DataTable CreateForeignKeysTable()
            {
                var table = new DataTable();
                foreach (var name in new[] { "application_name", "constraint_name", "child_schema_name", "child_table_name", "parent_schema_name", "parent_table_name", "child_column_name", "parent_column_name", "ordinal_position" })
                    table.Columns.Add(name);
                return table;
            }

            static DataTable CreateTriggersTable()
            {
                var table = new DataTable();
                foreach (var name in new[] { "application_name", "schema_name", "table_name", "trigger_name", "definition", "trigger_type", "trigger_timing", "is_enabled" })
                    table.Columns.Add(name);
                return table;
            }
            public DataTable TablesAndViews => Tables[TABLES_AND_VIEWS];
            public DataTable TableColumns => Tables[TABLE_COLUMNS];
            public DataTable PrimaryKeys => Tables[PRIMARY_KEYS];
            public DataTable UniqueConstraints => Tables[UNIQUE_CONSTRAINTS];
            public DataTable UniqueIndexes => Tables[UNIQUE_INDEXES];
            public DataTable ForeignKeys => Tables[FOREIGN_KEYS];
            public DataTable Triggers => Tables[TRIGGERS];
        }

        public class ProcsDataSet: DataSet
        {
            const string PROCS = "Procs";
            const string PROC_PARAMS = "ProcParams";
            const string PROC_COLUMNS = "ProcColumns";

            public ProcsDataSet(DataTable procs, DataTable procParams, DataTable procColumns)
            {
                procs.TableName = PROCS;
                Tables.Add(procs);

                procParams.TableName = PROC_PARAMS;
                Tables.Add(procParams);

                procColumns.TableName = PROC_COLUMNS;
                Tables.Add(procColumns);
            }
            public DataTable Procs => Tables[PROCS];
            public DataTable ProcParams => Tables[PROC_PARAMS];
            public DataTable ProcColumns => Tables[PROC_COLUMNS];
        }

        public class UdtsDataSet : DataSet
        {
            const string UDTS = "Udts";
            const string UDT_COLUMNS = "UdtColumns";

            public UdtsDataSet(DataTable udts, DataTable udtColumns)
            {
                udts.TableName = UDTS;
                Tables.Add(udts);

                udtColumns.TableName = UDT_COLUMNS;
                Tables.Add(udtColumns);
            }
            public DataTable Udts => Tables[UDTS];
            public DataTable UdtColumns => Tables[UDT_COLUMNS];
        }


        public BaseDataTableFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public abstract TablesAndViewsDataSet TablesAndViews { get; }
        public abstract ProcsDataSet Procs { get; }
        public abstract UdtsDataSet Udts { get; }
    }
}
