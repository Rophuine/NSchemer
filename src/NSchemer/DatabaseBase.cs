using System.Collections.Generic;

namespace NSchemer
{
    public abstract class DatabaseBase
    {
        public abstract List<Transition> Versions { get; }
        public abstract double DatabaseVersion { get; }
        public string VERSION_TABLE = "PEL_VERSIONING";
        public abstract string TIME_FUNCTION { get; }
        public abstract List<double> AllVersions { get; }

        public double LatestVersion
        {
            get
            {
                return Versions[Versions.Count - 1].VersionNumber;
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

                else
                {
                    success = success && AddRow(VERSION_TABLE, string.Format("{0},{1}", Versions[indexOfCurrentVersion].VersionNumber.ToString(), TIME_FUNCTION));  // Add each version number up to the passed in value
                }
            }
            return success;
        }

        public abstract bool AddRow(string tablename, string data);

    }
}
