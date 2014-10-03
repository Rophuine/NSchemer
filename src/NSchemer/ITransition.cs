namespace NSchemer
{
    public interface ITransition // This should really be a base class to enforce construction!
    {
        string Name { get; }
        string Description { get; }
        double VersionNumber { get; }
        bool Up(DatabaseBase database);
    }
}