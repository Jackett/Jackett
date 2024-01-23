using System;

namespace Jackett.Common.Helpers
{
    public static class Fluent
    {
        public static long Megabytes(this int megabytes)
        {
            return Convert.ToInt64(megabytes * 1024L * 1024L);
        }

        public static long Gigabytes(this int gigabytes)
        {
            return Convert.ToInt64(gigabytes * 1024L * 1024L * 1024L);
        }

        public static long Megabytes(this double megabytes)
        {
            return Convert.ToInt64(megabytes * 1024L * 1024L);
        }

        public static long Gigabytes(this double gigabytes)
        {
            return Convert.ToInt64(gigabytes * 1024L * 1024L * 1024L);
        }

        public static long Round(this long number, long level)
        {
            return Convert.ToInt64(Math.Floor((decimal)number / level) * level);
        }
    }
}
