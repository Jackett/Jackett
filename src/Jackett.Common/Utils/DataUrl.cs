using System;
using System.IO;
using MimeMapping;

namespace Jackett.Common.Utils
{
    public class DataUrlUtils
    {
        public static string ReadFileToDataUrl(string file)
        {
            var mime = MimeUtility.GetMimeMapping(file);
            return "data:" + mime + ";base64," + Convert.ToBase64String(File.ReadAllBytes(file));
        }

        public static string BytesToDataUrl(byte[] bytes, string mimeType = "image/jpg")
        {
            if (bytes == null)
                return null;
            return "data:" + mimeType + ";base64," + Convert.ToBase64String(bytes);
        }
    }
}
