﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace NSchemer.SqlServer
{
    public class SqlScriptTransition : ITransition
    {


        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, string embeddedResourceName)
            : this(versionNumber, null, Assembly.GetCallingAssembly(), embeddedResourceName)
        { }

        [Obsolete("NSchemer no longer uses name in transitions.")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, string name, string description, string embeddedResourceName)
            : this(versionNumber, description, Assembly.GetCallingAssembly(), embeddedResourceName) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, string description, string embeddedResourceName) 
            : this(versionNumber, description, Assembly.GetCallingAssembly(), embeddedResourceName) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, Assembly sourceAssembly, string embeddedResourceName, int timeout = SqlClientDatabase.DbDefaultCommandTimeout)
            : this(versionNumber, null, sourceAssembly, embeddedResourceName, timeout) { }


        [Obsolete("NSchemer no longer uses name in transitions.")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, string name, string description, Assembly sourceAssembly, string embeddedResourceName, int timeout = SqlClientDatabase.DbDefaultCommandTimeout)
            : this(versionNumber, description, sourceAssembly, embeddedResourceName, timeout) { }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public SqlScriptTransition(double versionNumber, string description, Assembly sourceAssembly, string embeddedResourceName, int timeout = SqlClientDatabase.DbDefaultCommandTimeout)
        {
            SourceAssembly = sourceAssembly ?? Assembly.GetCallingAssembly();
            EmbeddedResourceName = embeddedResourceName;
            Description = description;
            VersionNumber = versionNumber;
            Timeout = timeout;
        }

        public Assembly SourceAssembly { get; private set; }
        public string EmbeddedResourceName { get; private set; }
        public int Timeout { get; set; }

        public void Up(DatabaseBase database)
        {
            string script = ReadSqlFile(SourceAssembly, EmbeddedResourceName);
            var sqlDatabase = database as SqlClientDatabase;
            if (sqlDatabase == null)
                throw new InvalidOperationException("Tried to run a SQL script on a non-SQL database.");
            RunMultistepSqlScript(script, sqlDatabase, Timeout);
        }

        public static void RunMultistepSqlScript(string script, SqlClientDatabase sqlDatabase, int timeout = SqlClientDatabase.DbDefaultCommandTimeout)
        {
            var individualCommands = Regex.Split(string.Format(script, sqlDatabase.Catalog), @"^\s*GO\s*$",
                RegexOptions.Multiline)
                .Where(cmd => !string.IsNullOrWhiteSpace(cmd))
                .Select(cmd => cmd.Trim());


            foreach (string command in individualCommands)
            {
                try
                {
                    sqlDatabase.RunSql(command, timeout);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Failed to execute {0}", command), ex);
                }
            }
        }

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