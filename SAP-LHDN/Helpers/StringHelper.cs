using System;

namespace StringExtensions
{
    public static class StringHelper
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static string TruncateWithEllipsis(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            // Ensure there's enough space for ellipsis
            if (maxLength < 3)
            {
                return value.Substring(0, maxLength);
            }

            return value.Substring(0, maxLength - 3) + "...";
        }
    }
}