using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using NSchemer.SqlServer;
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

            db.Versions.Add(new CodeTransition(2, "", () => {}));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);

            db.Versions.Add(new CodeTransition(1, "", () => {}));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);
        }

        [Test]
        public void WhenAMissingUpdateThrows_NSchemerShouldNotLoopEndlessly()
        {
            var db = new UpdateTestDatabase("Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");

            db.Versions.Add(new CodeTransition(2, "", () => {}));
            db.Update();
            db.LatestVersion.ShouldBe(2);
            db.IsCurrent().ShouldBe(true);

            db.Versions.Add(new CodeTransition(1, "", () => throw new Exception())); // update which fails

            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;
            var timeout = 200;
            var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        db.Update();
                    }
                    catch
                    {
                    }
                },
                token);

            task.Wait(timeout, token).ShouldBe(true);
            db.IsCurrent().ShouldBe(false);
        }
    }

    public class UpdateTestDatabase : SqlClientDatabase
    {
        private readonly List<double> _appliedVersions = new List<double>();

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

        public override List<ITransition> Versions { get; } = new List<ITransition>();

        protected override void RunUpdate(ITransition transition)
        {
            transition.Up(this);
            _appliedVersions.Add(transition.VersionNumber);
        }

        protected override List<double> ReadAllAppliedVersions()
        {
            return _appliedVersions;
        }
    }
}