using System;
using System.Data;
using Microsoft.Data.SqlClient;
using NSchemer.SqlServer;
using NUnit.Framework;
using Shouldly;

namespace NSchemer.SystemTests
{
    public class SqlServerTest
    {
        // To run this test you need to provide a connection string to a SQL server with credentials which can create and delete databases.
        // Initial catalog will be ignored - the test harness will create a new one and clean up afterwards.
        private readonly string _sqlConnectionString = @"Server=.\;Database=;Trusted_Connection=True;";
        private string _sqlConnectionStringWithTempCatalog;
        private string _tempCatalog;

        [Test]
        public void RunUpgradeWithConnectionFactory()
        {
            Func<IDbConnection> connFactory = () =>
            {
                var conn = new SqlConnection(_sqlConnectionStringWithTempCatalog);
                return conn;
            };
            using var schema = new TestSchema(connFactory);
            RunUpgradeTestOnSchema(schema);
        }

        [Test]
        public void RunUpgradeWithConnectionString()
        {
            using var schema = new TestSchema(_sqlConnectionStringWithTempCatalog);
            RunUpgradeTestOnSchema(schema);
        }

        private static void RunUpgradeTestOnSchema(TestSchema schema)
        {
            schema.IsCurrent().ShouldBe(false);
            schema.Update();
            schema.IsCurrent().ShouldBe(true);
            SqlConnection.ClearPool(schema.Connection as SqlConnection);
        }

        [SetUp]
        public void Init()
        {
            _tempCatalog = $"nschemer_test_{Guid.NewGuid():N}";
            var builder = new SqlConnectionStringBuilder(_sqlConnectionString)
            {
                InitialCatalog = _tempCatalog
            };
            _sqlConnectionStringWithTempCatalog = builder.ConnectionString;

            SqlServerUtils.CreateDatabase(_sqlConnectionString, _tempCatalog);
        }

        [TearDown]
        public void CleanUp()
        {
            SqlServerUtils.DeleteDatabase(_sqlConnectionString, _tempCatalog);
        }
    }
}