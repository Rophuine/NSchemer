using System.Collections.Generic;
using NUnit.Framework;
using Shouldly;

namespace NSchemer.Tests
{
    public class SqlColumnTests
    {
        [Test]
        public void IdentityGeneratesCorrectSql()
        {
            var column = new SqlClientDatabase.Column("itemId", SqlClientDatabase.DataType.BIGINT).Identity(1, 1);

            var sql = column.GetSQL();

            sql.ShouldBe("[itemId] bigint NOT NULL IDENTITY(1,1)");
        }

        [Test]
        public void PrimaryKeyGeneratesCorrectSql()
        {
            var column = new SqlClientDatabase.Column("itemId", SqlClientDatabase.DataType.GUID).AsPrimaryKey();

            var sql = new ColumnTestDatabase("").CreateTableSql("item", new List<SqlClientDatabase.Column> {column});

            sql.ShouldBe("CREATE TABLE dbo.item ([itemId] uniqueidentifier NOT NULL, CONSTRAINT PK_item PRIMARY KEY CLUSTERED ([itemId]))");
        }
    }

    class ColumnTestDatabase : SqlClientDatabase
    {
        public ColumnTestDatabase(string connectionString) : base(connectionString)
        {
        }

        public override List<ITransition> Versions { get; }
    }
}