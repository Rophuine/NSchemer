using System;
using System.Collections.Generic;
using System.Linq;

namespace NSchemer
{
    public abstract class DatabaseBase
    {
        public abstract List<ITransition> Versions { get; }
        public abstract double DatabaseVersion { get; }
        public abstract string TimeFunction { get; }
        public abstract List<double> AllVersions { get; }
        public abstract int RunSql(string SqlString);
        public abstract int RunSql(string SqlString, int timeOut);

        public virtual string VersionTable { get { return "NSCHEMER_VERSION"; } }

        public double LatestVersion
        {
            get
            {
                return Versions.Max(v => v.VersionNumber);
            }
        }

        /// <summary>
        /// Forces NSchemer to mark all transitions up to and including the provided version number as having run. Does not actually run any transitions.
        /// You can use this to skip all earlier transitions if you generate a schema at a particular version (and thus don't want the updater to run earlier transitions).
        /// </summary>
        public void ForceCurrentVersionTo(double newVersion)
        {
            if (newVersion < DatabaseVersion) throw new Exception("Database is already at a higher version. NSchemer does not allow implicit downgrade of the version using this method."); // Don't allow implicit downgrade
            if (newVersion == DatabaseVersion) return; // Already there, nothing to do

            int indexOfCurrentVersion;
            for (indexOfCurrentVersion = 1; indexOfCurrentVersion < Versions.Count; indexOfCurrentVersion++)// ignore version 0
            {
                if (Versions[indexOfCurrentVersion].VersionNumber > newVersion)
                    break;
                AddRow(VersionTable, string.Format("{0},{1}", Versions[indexOfCurrentVersion].VersionNumber.ToString(), TimeFunction));  // Add each version number up to the passed in value
            }
        }

        public abstract void AddRow(string tablename, string data);

    }
}
