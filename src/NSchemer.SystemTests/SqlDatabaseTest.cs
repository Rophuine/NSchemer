using NUnit.Framework;

namespace NSchemer.SystemTests
{
    public class SqlDatabaseTest
    {
        [Test]
        public void RunUpgrade()
        {
            var schema = new TestSchema(@"Data Source=.\SQLEXPRESS;Initial Catalog=TestDb;Integrated Security=SSPI;Max Pool Size = 300;");
            schema.Update();
        }
    }
}