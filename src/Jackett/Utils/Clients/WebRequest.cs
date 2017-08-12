using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class WebRequest
    {
        public WebRequest()
        {
            PostData = new List<KeyValuePair<string, string>>();
            Type = RequestType.GET;
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public WebRequest(string url)
        {
            PostData = new List<KeyValuePair<string, string>>();
            Type = RequestType.GET;
            Url = url;
            Headers = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        }

        public string Url { get; set; }
        public IEnumerable<KeyValuePair<string, string>> PostData { get; set; }
        public string Cookies { get; set; }
        public string Referer { get; set; }
        public RequestType Type { get; set; }
        public string RawBody { get; set; }
        public bool? EmulateBrowser { get; set; }
        public Encoding Encoding { get; set; }

        /// <summary>
        /// Warning this is only implemented on HTTPWebClient currently!
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        public override bool Equals(System.Object obj)
        {
            if (obj is WebRequest)
            {
                var other = obj as WebRequest;
                var postDataSame = PostData == null && other.PostData == null;
                if (!postDataSame)
                {
                    if (!(PostData == null || other.PostData == null))
                    {
                        var ok = PostData.Count() == other.PostData.Count();
                        foreach (var i in PostData)
                        {
                            if (!other.PostData.Any(item => item.Key == i.Key))
                            {
                                ok = false;
                                break;
                            }

                            if (PostData.FirstOrDefault(item => item.Key == i.Key).Value != other.PostData.FirstOrDefault(item => item.Key == i.Key).Value)
                            {
                                ok = false;
                                break;
                            }
                        }

                        if (ok)
                        {
                            postDataSame = true;
                        }
                    }
                }

                return other.Url == Url &&
                       other.Referer == Referer &&
                       other.Cookies == Cookies &&
                       other.Type == Type &&
                       other.Encoding == Encoding &&
                       postDataSame;

            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum RequestType
    {
        GET,
        POST
    }
}
