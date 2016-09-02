using System.Globalization;
using System.Linq;

namespace WordTraining.Logic.Services
{
    public static class StringHelper
    {
        public static string RemoveCharacters(this string source, string charsToRemove)
        {
            return charsToRemove.Aggregate(source, (current, c) => current.Replace(c.ToString(CultureInfo.InvariantCulture), string.Empty));
        }
    }
}