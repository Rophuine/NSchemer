using System;
using System.Collections.Generic;
using System.Data;
using NUnit.Framework;
using Shouldly;

namespace NSchemer.Tests
{
    public class VersionTests
    {
        [Test]
        public void OutOfOrderVersions_StillReturnsCorrectMaxVersionNumber()
        {
            var db = new VersionTestDatabase("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");

            db.LatestVersion.ShouldBe(3);
        }
    }

    public class VersionTestDatabase : SqlClientDatabase {
        public VersionTestDatabase(string connectionString) : base(connectionString)
        {
        }

        public VersionTestDatabase(string connectionString, string schemaName) : base(connectionString, schemaName)
        {
        }

        public VersionTestDatabase(Func<IDbConnection> connectionProvider) : base(connectionProvider)
        {
        }

        public override List<ITransition> Versions
        {
            get { return new List<ITransition>
            {
                new CodeTransition(3, "Name1", "Desc1", null),
                new CodeTransition(2, "Name2", "Desc2", null)
            }; }
        }
    }
}