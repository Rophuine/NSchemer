namespace NSchemer
{
    public class CodeTransition : ITransition
    {
        public VersionUpdateHandler DoActualUpgrade;

        public CodeTransition(double versionNumber, string name, string description, VersionUpdateHandler upHandler)
        {
            Name = name;
            VersionNumber = versionNumber;
            Description = description;
            DoActualUpgrade = upHandler;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public double VersionNumber { get; set; }

        public bool Up(DatabaseBase database)
        {
            // Do the update, if it returns true, add the version entry in the database
            bool result = DoActualUpgrade();

            return result;
        }
    }
}