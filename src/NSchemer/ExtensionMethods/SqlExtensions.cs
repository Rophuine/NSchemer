namespace NSchemer.ExtensionMethods
{
    public static class SqlExtensions
    {
        public static string StripSquareBrackets(this string str)
        {
            return str.Replace("[", "").Replace("]", "");
        }
    }
}