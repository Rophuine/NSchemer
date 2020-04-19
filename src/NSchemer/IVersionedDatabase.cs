using System.Collections.Generic;

namespace NSchemer
{
    public interface IVersionedDatabase
    {
        string ConnectionString { get; set; }
        string SchemaName { get; set; }

        /// <summary>
        /// DO NOT use this to check up-to-dateness. Use IsCurrent()
        /// </summary>
        double DatabaseVersion { get; }

        List<double> AllVersions { get; }
        double LatestVersion { get; }
        bool IsCurrent();
        void AddRow(string tablename, string data);
        void Update();

        /// <summary>
        /// Delete a column from the given table. Do not add [] around the column name.
        /// </summary>
        void DeleteColumn(string tableName, string columnName);

        /// <summary>
        /// Run a SQL command with provision for setting a timeout value
        /// </summary>
        /// <param name="SqlString"></param>
        /// <param name="timeOut">The number of seconds to wait when executing the command (0 = indefinate)</param>
        /// <returns>Number of rows affected</returns>
        int RunSql(string SqlString, int timeOut);

        /// <summary>
        /// Runs a SQL command, returns the number of rows affected
        /// </summary>
        int RunSql(string SqlString);

        void ForceCurrentVersionTo(double newVersion);
    }
}
