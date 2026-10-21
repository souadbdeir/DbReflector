using DbReflector;
using NUnit.Framework;

namespace DbReflector.UnitTesting;

public class ColumnMetadataFlags
{
    [Test]
    public void FactoryPreservesAllMetadataFlags()
    {
        var column = ColumnInfo.CreateIntance(
            schema: "dbo",
            table: "Example",
            column: "Value",
            isPrimaryKey: false,
            isForeignKey: false,
            isKey: false,
            isNullable: false,
            dataType: "int",
            maxLength: null,
            numericPrecision: null,
            domainName: null,
            ordinalPosition: 1,
            isIdentity: true,
            isComputed: true,
            isRowVersion: true,
            hasDefaultConstraint: true);

        Assert.That(column.IsIdentity, Is.True);
        Assert.That(column.IsComputed, Is.True);
        Assert.That(column.IsRowVersion, Is.True);
        Assert.That(column.HasDefaultConstraint, Is.True);
    }

    [Test]
    public void LegacyFactoryDefaultsNewMetadataFlagsToFalse()
    {
        var column = ColumnInfo.CreateIntance(
            schema: "dbo",
            table: "Example",
            column: "Value",
            isPrimaryKey: false,
            isForeignKey: false,
            isKey: false,
            isNullable: false,
            dataType: "int",
            maxLength: null,
            numericPrecision: null,
            domainName: null,
            ordinalPosition: 1);

        Assert.That(column.IsIdentity, Is.False);
        Assert.That(column.IsComputed, Is.False);
        Assert.That(column.IsRowVersion, Is.False);
        Assert.That(column.HasDefaultConstraint, Is.False);
    }
}
