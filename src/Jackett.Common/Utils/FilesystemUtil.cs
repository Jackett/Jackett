using System;

namespace Jackett.Common.Utils
{
    public static class FilesystemUtil
    {
        public static string getFileNameWithoutExtension(string fileName)
        {
            var fileParts = fileName.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            if (fileParts.Length < 2)
            {
                return null;
            }
            return fileParts[0];
        }
    }
}
