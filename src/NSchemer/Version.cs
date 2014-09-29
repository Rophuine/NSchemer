using System;
using NSchemer.Interfaces;

namespace NSchemer
{
    public delegate bool VersionUpdateHandler();


    public class Transition
    {
        public string Name;
        public string Description;
        public double VersionNumber;
        public VersionUpdateHandler DoActualUpgrade;
        public Transition(double VersionNumber, string Name, string Description, VersionUpdateHandler UpHandler)
        {
            this.Name = Name;
            this.VersionNumber = VersionNumber;
            this.Description = Description;
            this.DoActualUpgrade = UpHandler;
        }
        public bool Up(DatabaseBase database)
        {
            // Do the update, if it returns true, add the version entry in the database
            bool result;
            if ((result = DoActualUpgrade()) == true)
            {
                // Add the version entry
                try
                {
                    result = result && database.AddRow(database.VERSION_TABLE, string.Format("{0},{1}", VersionNumber.ToString(), database.TIME_FUNCTION));
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Version upgrade to version {0} completed successfully, but the {1} table could not be updated to reflect this.", 
                        VersionNumber.ToString(), database.VERSION_TABLE), ex);
                }
            }
            
            return result;
        }
    }
}
