using System;

namespace fiitobot.GoogleSpreadsheet
{
    public static class TypeExtensions
    {
        public static object GetDefaultValue(this Type t)
        {
            if (t.IsValueType && Nullable.GetUnderlyingType(t) == null)
                return Activator.CreateInstance(t);
            if (t == typeof(string))
                return string.Empty;
            return null;
        }
    }
}
