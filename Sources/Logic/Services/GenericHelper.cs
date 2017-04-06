namespace WordTraining.Logic.Services
{
    public static class GenericHelper
    {
        public static T Of<T>(this object source)
        {
            if (source == null)
            {
                return default(T);
            }

            return (T)source;
        }
    }
}