using Moq;
using NUnit.Framework;

namespace NSchemer.Tests
{
    public class EmbeddedSqlFileTests
    {
        [Test]
        public void WhenThereIsNoFinalLinefeed_AfterTheFinalGo_TheStatementIsStillExtractedCorrectly()
        {
            var transition = new SqlScriptTransition(1, "", "", GetType().Assembly,
                GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");
            var database = new Mock<SqlClientDatabase>(MockBehavior.Strict, "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
            database.Setup(db => db.RunSql("Select something from athing")).Returns(1);
            database.Setup(db => db.RunSql("Select iGOr from anotherthing")).Returns(1);
            database.Setup(db => db.RunSql("Select * from somethingelse\r\norder by multilineQuery")).Returns(1);

            transition.Up(database.Object);
        }

        [Test]
        public void WhenNoAssemblyIsProvided_TheCallingAssemblyIsUsed()
        {
            var transition = new SqlScriptTransition(1, "", "",
                GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");
            var database = new Mock<SqlClientDatabase>(MockBehavior.Strict, "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
            database.Setup(db => db.RunSql("Select something from athing")).Returns(1);
            database.Setup(db => db.RunSql("Select iGOr from anotherthing")).Returns(1);
            database.Setup(db => db.RunSql("Select * from somethingelse\r\norder by multilineQuery")).Returns(1);

            transition.Up(database.Object);
        }
        
        [Test]
        public void WhenAssemblyIsProvidedAsNullDirectly_TheCallingAssemblyIsUsed()
        {
            var transition = new SqlScriptTransition(1, "", "", null,
                GetType().Namespace + ".SqlFileResources.BasicMultiStatement.sql");
            var database = new Mock<SqlClientDatabase>(MockBehavior.Strict, "Server=myServerAddress;Database=myDataBase;Trusted_Connection=True;");
            database.Setup(db => db.RunSql("Select something from athing")).Returns(1);
            database.Setup(db => db.RunSql("Select iGOr from anotherthing")).Returns(1);
            database.Setup(db => db.RunSql("Select * from somethingelse\r\norder by multilineQuery")).Returns(1);

            transition.Up(database.Object);
        }
    }
}