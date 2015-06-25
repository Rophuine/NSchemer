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

        public virtual string VersionTable { get { return "NSCHEMER_VERSION"; } }

        public double LatestVersion
        {
            get
            {
                return Versions.Max(v => v.VersionNumber);
            }
        }

        public bool ForceCurrentVersionTo(double newVersion)
        {
            bool success = true;
            if (newVersion < DatabaseVersion) return false; // Don't allow implicit downgrade
            if (newVersion == DatabaseVersion) return true; // Already there, nothing to do

            int indexOfCurrentVersion;
            for (indexOfCurrentVersion = 1; indexOfCurrentVersion < Versions.Count; indexOfCurrentVersion++)// ignore version 0
            {
                if (Versions[indexOfCurrentVersion].VersionNumber > newVersion)
                    break;
                success = success && AddRow(VersionTable, string.Format("{0},{1}", Versions[indexOfCurrentVersion].VersionNumber.ToString(), TimeFunction));  // Add each version number up to the passed in value
            }
            return success;
        }

        public abstract bool AddRow(string tablename, string data);

    }
}
