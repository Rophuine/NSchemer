using System;
using System.Collections.Generic;
using System.Data;
using NSchemer.Sql;
using NSchemer.SqlServer;

namespace NSchemer.SystemTests
{
    public class TestSchema : SqlClientDatabase
    {
        public TestSchema(string connectionString) : base(connectionString) { }

        public TestSchema(Func<IDbConnection> connectionFactory) : base(connectionFactory) { }

        public override List<ITransition> Versions
        {
            get
            {
                return new List<ITransition>
                {
                    new CodeTransition(1, "Initial Schema", BuildTheWorld),
                    new CodeTransition(2, "Script includes NOCOUNT ON", AddWidgets)
                };
            }
        }

        private void AddWidgets()
        {
            RunSql(@"
                        SET NOCOUNT ON
                        CREATE TABLE DBO.Widget (
                            WidgetId [int],
                            WidgetName [nvarchar](50)
                        ) ON [PRIMARY]
                    ");
        }

        private void BuildTheWorld()
        {
            CreateTable("Thing", new List<Column>
            {
                new Column("ThingName", DataType.STRING, 50),
                new Column("ThingId", DataType.INT)
            });
        }
    }
}