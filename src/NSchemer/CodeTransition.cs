using System;

namespace NSchemer
{
    public class CodeTransition : ITransition
    {
        public VersionUpdateHandler DoActualUpgrade;

        public CodeTransition(double versionNumber, VersionUpdateHandler upHandler)
            : this(versionNumber, null, upHandler)
        {
        }

        [Obsolete("NSchemer no longer uses name in transitions.")]
        public CodeTransition(double versionNumber, string description, string name, VersionUpdateHandler updateHandler) : this(versionNumber, description, updateHandler) { }

        public CodeTransition(double versionNumber, string description, VersionUpdateHandler upHandler)
        {
            VersionNumber = versionNumber;
            Description = description;
            DoActualUpgrade = upHandler;
        }

        public string Description { get; set; }
        public double VersionNumber { get; set; }

        public void Up(DatabaseBase database)
        {
            DoActualUpgrade();
        }
    }
}