﻿using System;
using System.IO;
using System.Reflection;
using System.Resources;

namespace NSchemer
{
    public class SqlScriptTransition : ITransition
    {
        public SqlScriptTransition(double versionNumber, string name, string description, string embeddedResourceName) 
            : this(versionNumber, name, description, null, embeddedResourceName) { }

        public SqlScriptTransition(double versionNumber, string name, string description, Assembly sourceAssembly,
            string embeddedResourceName)
        {
            SourceAssembly = sourceAssembly ?? GetType().Assembly;
            EmbeddedResourceName = embeddedResourceName;
            Description = description;
            Name = name;
            VersionNumber = versionNumber;
        }

        public Assembly SourceAssembly { get; private set; }
        public string EmbeddedResourceName { get; private set; }

        public bool Up(DatabaseBase database)
        {
            string script = ReadSqlFile(SourceAssembly, EmbeddedResourceName);
            var sqlDatabase = database as SqlClientDatabase;
            if (sqlDatabase == null)
                throw new InvalidOperationException("Tried to run a SQL script on a non-SQL database.");
            RunMultistepSqlScript(script, sqlDatabase);
            return true;
        }

        public static void RunMultistepSqlScript(string script, SqlClientDatabase sqlDatabase)
        {
            string[] individualCommands =
                string.Format(script, sqlDatabase.Catalog)
                    .Split(new[] {Environment.NewLine + "GO" + Environment.NewLine},
                        StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in individualCommands)
            {
                try
                {
                    sqlDatabase.RunSql(command);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Failed to execute {0}", command), ex);
                }
            }
        }

        public string Name { get; private set; }
        public string Description { get; private set; }
        public double VersionNumber { get; private set; }

        public static string ReadSqlFile(Assembly assembly, string resourceName)
        {
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new MissingManifestResourceException(string.Format("Unable to load {0} from {1}.",
                        resourceName, assembly.FullName));
                return new StreamReader(stream).ReadToEnd();
            }
        }
    }
}