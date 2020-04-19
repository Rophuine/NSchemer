using MartinCostello.SqlLocalDb;
using NUnit.Framework;
using Shouldly;

namespace NSchemer.SystemTests
{
    public class LocalDbTest
    {
        private SqlLocalDbApi _dbApi;
        private TemporarySqlLocalDbInstance _instance;
        private ISqlLocalDbInstanceManager _manager;


        [Test]
        public void RunUpgrade()
        {
            var schema = new TestSchema(_instance.ConnectionString);
            schema.IsCurrent().ShouldBe(false);
            schema.Update();
            schema.IsCurrent().ShouldBe(true);
        }

        [SetUp]
        public void Init()
        {
            _dbApi = new SqlLocalDbApi();
            _instance = _dbApi.CreateTemporaryInstance(true);
            _manager = _instance.Manage();
            _manager.Start();
        }

        [TearDown]
        public void Cleanup() 
        {
            _manager.Stop();
            _instance.Dispose();
            _dbApi.Dispose();
        }
    }
}