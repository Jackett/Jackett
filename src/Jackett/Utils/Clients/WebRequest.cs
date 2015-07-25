using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class WebRequest
    {
        public WebRequest()
        {
            PostData = new Dictionary<string, string>();
            Type = RequestType.GET;
        }

        public string Url { get; set; }
        public Dictionary<string, string> PostData { get; set; }
        public string Cookies { get; set; }
        public string Referer { get; set; }
        public RequestType Type { get; set; }

        public override bool Equals(System.Object obj)
        {
            if(obj is WebRequest)
            {
                var other = obj as WebRequest;
                var postDataSame = PostData == null && other.PostData == null;
                if (!postDataSame)
                {
                    if (!(PostData == null || other.PostData == null))
                    {
                        var ok = PostData.Count == other.PostData.Count;
                        foreach(var i in PostData)
                        {
                            if (!other.PostData.ContainsKey(i.Key))
                            {
                                ok = false;
                                break;
                            }

                            if(PostData[i.Key] != other.PostData[i.Key])
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
                       postDataSame;

            } else
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
