using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Jackett.Common.Utils.Clients
{
    public class WebResult
    {
        private string _contentString;
        private Encoding _encoding;

        public Encoding Encoding
        {
            get
            {
                if (_encoding != null)
                    return _encoding;
                if (Request.Encoding != null)
                    _encoding = Request.Encoding;
                else if (Headers.ContainsKey("content-type"))
                {
                    var charsetRegexMatch = Regex.Match(
                        Headers["content-type"][0], @"charset=([\w-]+)", RegexOptions.Compiled);
                    if (charsetRegexMatch.Success)
                    {
                        var charset = charsetRegexMatch.Groups[1].Value;
                        try
                        {
                            _encoding = Encoding.GetEncoding(charset);
                        }
                        catch (ArgumentException)
                        {
                            // Encoding not found or not enabled on current machine.
                        }
                    }
                }

                _encoding ??= Encoding.UTF8;
                return _encoding;
            }
            set => _encoding = value;
        }

        public byte[] ContentBytes { get; set; }
        public HttpStatusCode Status { get; set; }
        public string Cookies { get; set; }
        public string RedirectingTo { get; set; }
        public WebRequest Request { get; set; }

        public Dictionary<string, string[]> Headers { get; protected set; } =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        public string ContentString
        {
            get
            {
                if (_contentString != null)
                    return _contentString;
                if (ContentBytes == null)
                    return null;
                _contentString = Encoding.GetString(ContentBytes);
                return _contentString;
            }
            set => _contentString = value;
        }

        public bool HasHttpError => (int)Status >= 400;

        public bool HasHttpServerError => (int)Status >= 500;

        public bool IsRedirect => Status == HttpStatusCode.Redirect ||
                                  Status == HttpStatusCode.RedirectKeepVerb ||
                                  Status == HttpStatusCode.RedirectMethod ||
                                  Status == HttpStatusCode.Found ||
                                  Status == HttpStatusCode.MovedPermanently ||
                                  Headers.ContainsKey("Refresh");
    }
}
