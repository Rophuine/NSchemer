using Moq;
using NSchemer.Sql;
using NUnit.Framework;

namespace NSchemer.Tests
{
    public class EmbeddedSqlFileTests
    {
        private const string
            Script1 = "Select something from athing",
            Script2 = "Select iGOr from anotherthing",
            Script3 = "Select * from somethingelse\r\norder by multilineQuery";

        private Mock<SqlClientDatabase> CreateDatabaseForBasicMultiStatementScriptTests()
        {
            var database = new Mock<SqlClientDatabase>(MockBehavior.Strict, "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
            database.Setup(db => db.RunSql(Script1, It.IsAny<int>())).Returns(1);
            database.Setup(db => db.RunSql(Script2, It.IsAny<int>())).Returns(1);
            database.Setup(db => db.RunSql(Script3, It.IsAny<int>())).Returns(1);

            return database;
        }

        [Test]
        public void WhenThereIsNoFinalLinefeed_AfterTheFinalGo_TheStatementIsStillExtractedCorrectly()
        {
            var transition = new SqlScriptTransition(1, "", "", GetType().Assembly, GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");

            var database = CreateDatabaseForBasicMultiStatementScriptTests();
            transition.Up(database.Object);
        }

        [Test]
        public void WhenNoAssemblyIsProvided_TheCallingAssemblyIsUsed()
        {
            var transition = new SqlScriptTransition(1, "", "", GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");

            var database = CreateDatabaseForBasicMultiStatementScriptTests();
            transition.Up(database.Object);
        }
        
        [Test]
        public void WhenAssemblyIsProvidedAsNullDirectly_TheCallingAssemblyIsUsed()
        {
            var transition = new SqlScriptTransition(1, "", "", null, GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");

            var database = CreateDatabaseForBasicMultiStatementScriptTests();
            transition.Up(database.Object);
        }

        [Test]
        public void WhenATimeoutIsProvidedOnTheScript_TheTimeoutIsPassedWhenRunningSql()
        {
            // arrange
            const int timeout = 180;

            var transition = new SqlScriptTransition(1, "", "", GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql")
            {
                Timeout = timeout
            };

            var database = CreateDatabaseForBasicMultiStatementScriptTests();

            // act
            transition.Up(database.Object);

            // assert
            database.Verify(db => db.RunSql(It.IsAny<string>(), timeout), Times.Exactly(3));
        }

        [Test]
        public void WhenNoTimeoutIsProvidedOnTheScript_TheDefaultTimeoutIsUsed()
        {
            // arrange
            var transition = new SqlScriptTransition(1, "", "", GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");
            var database = CreateDatabaseForBasicMultiStatementScriptTests();

            // act
            transition.Up(database.Object);

            // assert
            database.Verify(db => db.RunSql(It.IsAny<string>(), SqlClientDatabase.DbDefaultCommandTimeout), Times.Exactly(3));
        }
    }
}