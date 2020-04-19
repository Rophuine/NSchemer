using Microsoft.Data.SqlClient;

namespace NSchemer.SqlServer
{
    public class SqlServerUtils
    {
        public static void CreateDatabase(string connectionString, string name)
        {
            RunCommand(connectionString, $"CREATE DATABASE [{name}]");
        }

        public static void DeleteDatabase(string connectionString, string name)
        {
            RunCommand(connectionString, $"DROP DATABASE [{name}]");
        }

        private static void RunCommand(string connectionString, string commandText)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = commandText;
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }
    }
}