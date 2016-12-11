namespace NSchemer.ExtensionMethods
{
    internal static class SqlExtensions
    {
        internal static string StripSquareBrackets(this string str)
        {
            return str.Replace("[", "").Replace("]", "");
        }
    }
}