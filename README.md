# DbReflector
A class library that represents database objects, handy for code generation.

```csharp
using DbReflector;
using DbReflector.Common;
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var cs = @"server=.\sqlexpress;database=AdventureWorksCS;Trusted_Connection=True;";
            ISchemaInfo ShemaInfo = SchemaInfoFactory.CreateInstance(cs, CSharpTableInfoFormatter.CreateInstance(), "myapp").Reflect();
            ITableInfo table = ShemaInfo.Tables["Person.Address"];

            Console.WriteLine(table.TableFQN);
            Console.WriteLine("\nAll columns");
            foreach (var col in table.Columns.Values)
            {
                string dbtype = $"{col.DataType}" + (col.MaxLength==null? string.Empty : $"({col.MaxLength})");
                Console.WriteLine($"\n\nColumn: {col.ColumnName}");
                Console.WriteLine($"Domain: {col.DomainName}");
                Console.WriteLine($"Db Type: {dbtype}");
                Console.WriteLine($"C# Type: {col.CSharpType}");
                Console.WriteLine($"Primary Key? {col.IsPrimaryKey}");
                Console.WriteLine($"Foreign Key? {col.IsForeignKey}");
                Console.WriteLine($"Nullable? {col.IsNullable}");
                
            }
        }
    }
}
```

## Recursive relationship lookup

Call the API with a table name and primary-key column:

```csharp
var graph = schemaInfo.GetRelatedTablesRecursively("dbo.Parent", "Id");
```

The returned graph contains:

- `RootTable`: the starting table.
- `Tables`: every connected table found recursively, including tables the root references and tables that reference it.
- `ForeignKeys`: actual foreign-key relationships, including constraint name, child table, parent table, and ordered child-to-parent column mappings.
- `RelationshipCandidates`: possible name-based relationships where no foreign-key constraint exists. Each candidate includes the child table/column, parent table/column, and `IsInferred = true`.
- Each table in `Tables` also contains its columns, primary key, unique constraints, unique indexes, outgoing foreign keys, and triggers.

If the starting table or primary-key column is not found, the API returns an empty graph.
