using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using NSchemer.Interfaces;

namespace NSchemer
{
    public abstract class SqlClientDatabase : DatabaseBase, IDisposable, IVersionedDatabase
    {
        public const int DbDefaultCommandTimeout = -1;

        public string ConnectionString { get; set; }
        protected Lazy<IDbConnection> _connection;

        protected Func<string, IDbConnection, IDbTransaction, IDbCommand> TransactionalCommandFactory 
            = (sql, connection, transaction) => new SqlCommand(sql, connection as SqlConnection, transaction as SqlTransaction);
        protected Func<string, IDbConnection, IDbCommand> CommandFactory
            = (sql, connection) => new SqlCommand(sql, connection as SqlConnection);
            
        SqlTransaction CurrentTransaction = null;
        public string SchemaName { get; set; }

        protected string SchemaNameWithDotOrBlank
        {
            get
            {
                if (string.IsNullOrWhiteSpace(SchemaName)) return "";
                return SchemaName + ".";
            }
        }

        /// <summary>
        /// DO NOT use this to check up-to-dateness. Use IsCurrent()
        /// </summary>
        public override double DatabaseVersion
        {
            get
            {
                return ReadHighestVersionEntry();
            }
        }

        public override List<double> AllVersions
        {
            get { return ReadAllAppliedVersions(); }
        }

        public override string TimeFunction
        {
            get { return "GetDate()"; }
        }

        public string Catalog
        {
            get
            {
                var csb = new SqlConnectionStringBuilder(ConnectionString);
                return csb.InitialCatalog;
            }
        }

        protected IDbConnection Connection
        {
            get { return _connection.Value; }
        }

        public enum DataType
        {
            // Markup each datatype with the correct SQL identifier to create it
            // If the datatype requires a size, add the text "(size)" in the correct spot - the "size" part will be replaced with the length specified
            [Description("nvarchar(size)")]
            STRING,
            [Description("uniqueidentifier")]
            GUID,
            [Description("integer")]
            INT,
            [Description("datetime")]
            DATETIME,
            [Description("bit")]
            BIT,
            [Description("uniqueidentifier")]
            UNIQUEID,
            [Description("tinyint")]
            TINYINT,
            [Description("float")]
            FLOAT,
            [Description("varbinary(max)")]
            BINARY,
            [Description("smallint")]
            SMALLINT,
            [Description("bigint")]
            BIGINT
        }
        public class Column
        {
            public string name;
            public DataType dataType;
            public int length;
            public bool nullable;
            public string defaultSqlData = null;

            public virtual string GetSQL(bool forceNullable = false)
            {
                // Gives the DDL to generate this row
                    string datatypeString = null;
                    // This retrieves the datatype string from the enum markup above into datatypeString
                    MemberInfo[] memberInfo = typeof(DataType).GetMember(dataType.ToString());
                    if (memberInfo != null && memberInfo.Length > 0)
                    {
                        object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                        if (attrs != null && attrs.Length > 0)
                        {
                            datatypeString = ((DescriptionAttribute)attrs[0]).Description;
                        }
                    }
                    if (datatypeString == null)
                        throw new Exception(string.Format("Cannot determine correct datatype string while creating field {0}.", name));
                    string result = string.Format("[{0}] {1}", name, datatypeString);
                    if (result.Contains("(size)"))
                    {
                        result = result.Replace("(size)", string.Format("({0})", length.ToString()));
                    }
                if (nullable || forceNullable)
                        result += " NULL";
                    else
                        result += " NOT NULL";

                    return result;
                }
            public Column(string name, DataType dataType)
                : this(name, dataType, 0)
            { }
            public Column(string name, DataType dataType, int length)
            {
                this.name = name;
                this.dataType = dataType;
                this.length = length;
                this.nullable = true;
            }
            public Column(string name, DataType dataType, int length, bool nullable, string defaultSqlData)
                : this(name, dataType, length)
            {
                this.nullable = nullable;
                this.defaultSqlData = defaultSqlData;
            }
            public Column(string name, DataType dataType, bool nullable, string defaultSqlData) : this(name, dataType, 0, nullable, defaultSqlData) { }
        }

        public virtual bool IsCurrent()
        {
            List<ITransition> missingUpdates = new List<ITransition>();
            var allVersions = ReadAllAppliedVersions();
            foreach (ITransition v in Versions)
            {
                if (!allVersions.Contains(v.VersionNumber))
                    return false;
            }
            return true;
        }

        public override bool AddRow(string tablename, string data)
        {
            return AddRow(tablename, data, false);
        }

        public bool AddRow(string tablename, string data, bool checkInsertCount)
        {
            string sql = string.Format("INSERT INTO {0}{1} VALUES ({2})", SchemaNameWithDotOrBlank, tablename, data);
            int rows = RunSql(sql);
            if (checkInsertCount && (rows == 0 || rows > 1)) return false;
            return true;            
        }

        public bool AddRow(string tableName, string[] columns, string[] sqlFormattedData, bool checkInsertCount = false)
        {
            string sql = string.Format("INSERT INTO {0}{1} ({2}) VALUES ({3})",
                SchemaNameWithDotOrBlank,
                tableName,
                string.Join(",", columns.Select(column => "[" + column + "]")),
                string.Join(",", sqlFormattedData));
            int rows = RunSql(sql);
            if (checkInsertCount && (rows == 0 || rows > 1)) return false;
            return true;
        }

        public void Update()
        {
            // This brings the current database up to date. Use with care! It should probably not be accessible to end users, but only from Admin tools.
            bool appliedUpdate = true;

            // first apply all updates that are missing from this database's list of versions
            List<ITransition> missingUpdates = new List<ITransition>();
            foreach (ITransition v in Versions)
            {
                if (!AllVersions.Contains(v.VersionNumber) && v.VersionNumber < DatabaseVersion)
                    missingUpdates.Add(v);
            }
            missingUpdates.Sort((x, y) => x.VersionNumber.CompareTo(y.VersionNumber));
            foreach (ITransition v in missingUpdates)
                if (!RunUpdate(v)) throw new Exception(string.Format("Version number {0} reported an error applying the update.", v.VersionNumber));

            // now update the remaining 
            while (!IsCurrent() && appliedUpdate)
                // With the below orderby, there's now no point in doing these in two separate passes - but I want some more tests before I make that change
                foreach (ITransition v in Versions.OrderBy(v => v.VersionNumber))
                {
                    if (v.VersionNumber > DatabaseVersion)
                    {
                        appliedUpdate = RunUpdate(v);
                        if (!appliedUpdate) throw new Exception(string.Format("Version number {0} reported an error applying the update.", v.VersionNumber));
                    }
                }
        }

        protected virtual bool RunUpdate(ITransition transition)
        {
            if (transition.VersionNumber <= 0) throw new Exception("NSchemer expects all version numbers to be greater than zero.");
            var result = transition.Up(this);
            if (result)
            {
                // Add the version entry
                try
                {
                    result = AddRow(VersionTable, string.Format("{0},{1}", transition.VersionNumber, TimeFunction));
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Version upgrade to version {0} completed successfully, but the {1} table could not be updated to reflect this.",
                        transition.VersionNumber, VersionTable), ex);
                }
            }
            return result;
        }

        /// <summary>
        /// Delete a column from the given table. Do not add [] around the column name.
        /// </summary>
        public void DeleteColumn(string tableName, string columnName)
        {
            DeleteColumn(tableName, columnName, true);
        }
        
        /// <summary>
        /// Delete a column from the given table. Do not add [] around the column name.
        /// </summary>
        public void DeleteColumn(string tableName, string columnName, bool checkIfColumnExistsFirst = true)
        {
            var sql = string.Format("ALTER TABLE {0}{1} DROP COLUMN [{2}]", SchemaNameWithDotOrBlank, tableName, columnName);
            if (checkIfColumnExistsFirst) sql = "IF EXISTS(SELECT * FROM sys.columns WHERE Name = N'{2}' AND Object_ID = Object_ID(N'{1}')) " + sql;
            RunSql(sql);
        }

        public bool AddColumn(string tablename, Column column, int dataUpdateTimeout = DbDefaultCommandTimeout)
        {
            try
            {
                string sql = string.Format("ALTER TABLE {0}[{1}] ADD {2}", SchemaNameWithDotOrBlank, tablename, column.GetSQL(true));
                RunSql(sql);
                if (column.defaultSqlData != null && column.defaultSqlData != "")
                {
                    sql = string.Format("UPDATE {0}[{1}] SET {2}={3}", SchemaNameWithDotOrBlank, tablename, column.name, column.defaultSqlData);
                    RunSql(sql, dataUpdateTimeout);
                }
                if (column.nullable == false)
                {
                    sql = string.Format("ALTER TABLE {0}[{1}] ALTER COLUMN {2}", SchemaNameWithDotOrBlank, tablename, column.GetSQL());
                    RunSql(sql);
                }
            }
            catch (SqlException ex)
            {
                if (!(ex.Message.Contains("Column names in each table must be unique") && ex.Message.Contains("more than once")))
                    throw ex;
            }
            return true;
        }
        public void CreateTable(string TableName, List<Column> cols)
        {
            if (!TableExists(TableName))
            {
                string sql = string.Format("CREATE TABLE {0}{1} (", SchemaNameWithDotOrBlank, TableName);
                bool first = true;
                foreach (Column c in cols)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sql += ", ";
                    }
                    sql += c.GetSQL();
                }
                sql += ")";
                RunSql(sql);
            }
            else
            {
                throw new Exception(string.Format("Table {0} already exists, unable to create it.", TableName));
            }
        }
        public void RenameField(string TableName, string CurrentName, string NewName)
        {
            string sql = string.Format("exec sp_rename '{0}{1}.{2}', '{3}'", SchemaNameWithDotOrBlank, TableName, CurrentName, NewName);
            RunSql(sql);
        }
        public void ChangeDatatype(string TableName, string Column, DataType newtype)
        {
            ChangeDatatype(TableName, Column, newtype, 0);
        }
        public void ChangeDatatype(string TableName, string Column, DataType newtype, int size)
        {
            string datatypeString = null;
            MemberInfo[] memberInfo = typeof(DataType).GetMember(newtype.ToString());
            if (memberInfo != null && memberInfo.Length > 0)
            {
                object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (attrs != null && attrs.Length > 0)
                {
                    datatypeString = ((DescriptionAttribute)attrs[0]).Description;
                }
            }
            if (datatypeString == null)
                throw new Exception(string.Format("Cannot determine correct datatype string while changing field {0}.", Column));
            if (datatypeString.Contains("(size)"))
            {
                datatypeString = datatypeString.Replace("(size)", string.Format("({0})", size.ToString()));
            }
            string sql = string.Format("ALTER TABLE {0}{1} ALTER COLUMN [{2}] {3}", SchemaNameWithDotOrBlank, TableName, Column, datatypeString);
            RunSql(sql);
        }

        private double ReadHighestVersionEntry()
        {
            List<double> allVersions = ReadAllAppliedVersions();
            if (allVersions.Count > 0)
                return allVersions.Max();
            return 0;
        }

        /// <summary>
        /// Returns a list of all the versions applied to this database
        /// either from the initial scripting or through applying updates
        /// </summary>
        /// <returns></returns>
        protected virtual List<double> ReadAllAppliedVersions()
        {

            if (TableExists(VersionTable))
            {
                List<double> versionList = new List<double>();

                string sql = string.Format("SELECT VERSIONNUMBER FROM {0}{1}", SchemaNameWithDotOrBlank, VersionTable);
                using (SqlDataReader dr = RunQuery(sql))
                {
                    while (dr.Read())
                    {
                        versionList.Add(Convert.ToDouble(dr["VERSIONNUMBER"]));
                    }
                }
                return versionList;
            }
            else
            {
                CreateTable(VersionTable, new List<Column>() {
                    new Column("VERSIONNUMBER", DataType.FLOAT),
                    new Column("DATEAPPLIED", DataType.DATETIME)
                });
                string sql = string.Format("INSERT INTO {0}{1} (VERSIONNUMBER, DATEAPPLIED) VALUES (0, GetDate())", SchemaNameWithDotOrBlank, VersionTable);
                RunSql(sql);
                return new List<double>() { 0 };
            }
        }

        public SqlClientDatabase(string connectionString)
            : this(connectionString, "dbo")
        { }
        public SqlClientDatabase(string connectionString, string schemaName)
        {
            SchemaName = schemaName;
            var csb = new SqlConnectionStringBuilder(connectionString) {MultipleActiveResultSets = true};
            ConnectionString = csb.ToString();
            _connection = new Lazy<IDbConnection>(() =>
            {
                var conn = new SqlConnection(ConnectionString);
                conn.Open();
                return conn;
            });
        }

        public SqlClientDatabase(Func<IDbConnection> connectionProvider)
        {
            _connection = new Lazy<IDbConnection>(() =>
            {
                var conn = connectionProvider();
                if (conn.State != ConnectionState.Open) conn.Open();
                return conn;
            });
        }

        public void Dispose()
        {
            //close and dispose in dispose rather than the finalizer http://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqldatareader.close.aspx
            if (Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        protected IDbCommand NewCommand(string SqlString)
        {
            IDbCommand newCommand;
            if (CurrentTransaction == null)
            {
                newCommand = CommandFactory(SqlString, Connection);
            }
            else
            {
                newCommand = TransactionalCommandFactory(SqlString, Connection, CurrentTransaction);
            }
            newCommand.CommandType = System.Data.CommandType.Text;
            return newCommand;
        }
        /// <summary>
        /// Run a SQL command with provision for setting a timeout value
        /// </summary>
        /// <param name="SqlString"></param>
        /// <param name="timeOut">The number of seconds to wait when executing the command (0 = indefinate)</param>
        /// <returns>Number of rows affected</returns>
        public virtual int RunSql(string SqlString, int timeOut)
        {
            IDbCommand comm = NewCommand(SqlString);
            if (timeOut > DbDefaultCommandTimeout)
                comm.CommandTimeout = timeOut;

            return comm.ExecuteNonQuery();
        }

        /// <summary>
        /// Runs a SQL command, returns the number of rows affected
        /// </summary>
        public virtual int RunSql(string SqlString)
        {
            return RunSql(SqlString, DbDefaultCommandTimeout);
        }

        protected bool TableExists(string TableName)
        {
            string checkTable = String.Format("IF OBJECT_ID('{0}{1}', 'U') IS NOT NULL SELECT 'true' ELSE SELECT 'false'", SchemaNameWithDotOrBlank, TableName);
            return Convert.ToBoolean(RunScalar(checkTable));
        }
        protected object RunScalar(string Sql)
        {
            IDbCommand command = CommandFactory(Sql, Connection);
            command.CommandType = System.Data.CommandType.Text;
            return command.ExecuteScalar();
        }
        public SqlDataReader RunQuery(string sql)
        {
            SqlConnection tmpConnection = new SqlConnection(this.ConnectionString);
            tmpConnection.Open();
            SqlCommand command = new SqlCommand(sql, tmpConnection);
            command.CommandType = System.Data.CommandType.Text;
            return command.ExecuteReader(System.Data.CommandBehavior.CloseConnection);
        }

        protected string GetPKName(string pkTable)
        {
            var query = new StringBuilder();
            query.AppendLine("SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS");
            query.AppendLine(string.Format("WHERE TABLE_NAME = '{0}'", pkTable));
            query.AppendLine("AND CONSTRAINT_TYPE = 'PRIMARY KEY'");
            return (string)RunScalar(query.ToString());
        }

        protected bool TryGetUQName(string UQTable, string UQColumn, out string UQName)
        {
            var res = RunScalar(string.Format("select TC.CONSTRAINT_NAME " +
                   "from information_schema.table_constraints TC " +
                   "inner join information_schema.constraint_column_usage CC on TC.Constraint_Name = CC.Constraint_Name " +
                   "where TC.constraint_type = 'Unique' " +
                   "and TC.TABLE_NAME = '{0}' and COLUMN_NAME = '{1}'", UQTable, UQColumn));

            UQName = res as string;
            if (!string.IsNullOrWhiteSpace(UQName))
            {
                return true;
            }

            return false;
        }

        protected bool TryGetFKName(string FKTable, string FKColumn, out string FKName)
        {
            try
            {
                FKName = QueryFKView(FKTable, FKColumn);
                return !string.IsNullOrEmpty(FKName) ;
            }
            catch
            {
                try
                {
                    CreateFKView();
                    FKName = QueryFKView(FKTable, FKColumn);
                    return !string.IsNullOrEmpty(FKName);
                }
                catch
                {
                    FKName = null;
                    return false;
                }
            }
        }

        [Obsolete("Use TryGetFKName instead - this method has undefined behaviour if the FK doesn't exist, which has happened even when it really *should* exist.")]
        protected string GetFKName(string FKTable, string FKColumn)
        {
            try
            {
                return QueryFKView(FKTable, FKColumn);
            }
            catch
            {
                CreateFKView();
                return QueryFKView(FKTable, FKColumn);
            }
        }

        private void CreateFKView()
        {
            RunSql("create view ForeignKeyInformation as (SELECT      KCU1.CONSTRAINT_NAME AS 'FK_CONSTRAINT_NAME'   , KCU1.TABLE_NAME AS 'FK_TABLE_NAME' " +
                    ", KCU1.COLUMN_NAME AS 'FK_COLUMN_NAME'   , KCU1.ORDINAL_POSITION AS 'FK_ORDINAL_POSITION'   , KCU2.CONSTRAINT_NAME AS 'UQ_CONSTRAINT_NAME' " +
                    ", KCU2.TABLE_NAME AS 'UQ_TABLE_NAME'   , KCU2.COLUMN_NAME AS 'UQ_COLUMN_NAME'   , KCU2.ORDINAL_POSITION AS 'UQ_ORDINAL_POSITION' " +
                    "FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS RC JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU1 ON KCU1.CONSTRAINT_CATALOG = RC.CONSTRAINT_CATALOG " +
                    "AND KCU1.CONSTRAINT_SCHEMA = RC.CONSTRAINT_SCHEMA   AND KCU1.CONSTRAINT_NAME = RC.CONSTRAINT_NAME JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KCU2 " +
                    "ON KCU2.CONSTRAINT_CATALOG = RC.UNIQUE_CONSTRAINT_CATALOG    AND KCU2.CONSTRAINT_SCHEMA = RC.UNIQUE_CONSTRAINT_SCHEMA   AND KCU2.CONSTRAINT_NAME = " +
                    "RC.UNIQUE_CONSTRAINT_NAME   AND KCU2.ORDINAL_POSITION = KCU1.ORDINAL_POSITION)");
        }
        private string QueryFKView(string FKTable, string FKColumn)
        {
            return (string)RunScalar(string.Format("select FK_CONSTRAINT_NAME FROM ForeignKeyInformation " +
                "WHERE FK_TABLE_NAME = '{0}' and FK_COLUMN_NAME = '{1}'", FKTable, FKColumn));
        }
    }
}
