namespace NSchemer
{
    public interface ITransition // This should really be a base class to enforce construction!
    {
        string Description { get; }
        double VersionNumber { get; }
        void Up(DatabaseBase database);
    }
}