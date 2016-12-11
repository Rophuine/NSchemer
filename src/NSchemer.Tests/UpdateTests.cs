using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NSchemer.Sql;
using NUnit.Framework;
using Shouldly;

namespace NSchemer.Tests
{
    public class UpdateTests
    {
        [Test]
        public void MissingUpdatesShouldBeRun()
        {
            var db = new UpdateTestDatabase("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");

            db.Versions.Add(new CodeTransition(2, "", "", () => true));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);

            db.Versions.Add(new CodeTransition(1, "", "", () => true));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);
        }

        [Test]
        public void WhenAMissingUpdateReturnsFalse_NSchemerShouldNotLoopEndlessly()
        {
            var db = new UpdateTestDatabase("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");

            db.Versions.Add(new CodeTransition(2, "", "", () => true));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);

            db.Versions.Add(new CodeTransition(1, "", "", () => false));    // update which fails

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            int timeout = 200;
            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    db.Update();
                }
                catch {}
            },
            token);

            task.Wait(timeout, token).ShouldBe(true);
            db.IsCurrent().ShouldBe(false);
        }
    }

    public class UpdateTestDatabase : SqlClientDatabase
    {
        protected override bool RunUpdate(ITransition transition)
        {
            var result = transition.Up(this);
            if (result) _appliedVersions.Add(transition.VersionNumber);
            return result;
        }

        public UpdateTestDatabase(string connectionString) : base(connectionString)
        {
        }

        public UpdateTestDatabase(string connectionString, string schemaName) : base(connectionString, schemaName)
        {
        }

        public UpdateTestDatabase(Func<IDbConnection> connectionProvider)
            : base(connectionProvider)
        {
        }

        private readonly List<ITransition> _versions = new List<ITransition>();
        public override List<ITransition> Versions {get {return _versions;}}

        private readonly List<double> _appliedVersions = new List<double>(); 
        protected override List<double> ReadAllAppliedVersions()
        {
            return _appliedVersions;
        }
    }
}