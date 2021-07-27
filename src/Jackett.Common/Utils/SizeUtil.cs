using System.Text.RegularExpressions;

namespace Jackett.Common.Utils
{
    public static class SizeUtil
    {
        const long MULTIPLIER_TB = 1024L * 1024L * 1024L * 1024L;
        const long MULTIPLIER_GB = 1024L * 1024L * 1024L;
        const long MULTIPLIER_MB = 1024L * 1024L;
        const long MULTIPLIER_KB = 1024L;
        const long MULTIPLIER_B = 0L;

        public static bool TryConvertHumanReadableToBytes(string humanReadableSize, out long sizeInBytes)
        {
            long multiplier = -1;
            if (humanReadableSize.EndsWith("TB"))
            {
                multiplier = MULTIPLIER_TB;
            }
            else if (humanReadableSize.EndsWith("GB"))
            {
                multiplier = MULTIPLIER_GB;
            }
            else if (humanReadableSize.EndsWith("MB"))
            {
                multiplier = MULTIPLIER_MB;
            }
            else if (humanReadableSize.EndsWith("KB"))
            {
                multiplier = MULTIPLIER_KB;
            }
            else if (Regex.IsMatch(humanReadableSize, "[0-9]B$"))
            {
                multiplier = MULTIPLIER_B;
            }

            if (multiplier != -1)
            {
                var match = Regex.Match(humanReadableSize, "^[0-9.]+");
                if (match.Success)
                {
                    var rawDigits = match.Value;
                    if (double.TryParse(rawDigits, out double digits))
                    {
                        sizeInBytes = (long)(digits * multiplier);
                        return true;
                    }
                }
            }

            sizeInBytes = 0;
            return false;
        }
    }
}
