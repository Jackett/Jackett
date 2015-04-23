using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
	public static class CookieContainerExtensions
	{
		public static void FillFromJson (this CookieContainer cookies, Uri uri, JArray json)
		{
			foreach (string cookie in json) {

				var w = cookie.Split ('=');
				if (w.Length == 1)
					cookies.Add (uri, new Cookie{ Name = cookie.Trim () });
				else
					cookies.Add (uri, new Cookie (w [0].Trim (), w [1].Trim ()));
			}
		}

		public static JArray ToJson (this CookieContainer cookies, Uri baseUrl)
		{
			return new JArray ((
			    from cookie in cookies.GetCookies (baseUrl).Cast<Cookie> ()
			    select cookie.Name.Trim () + "=" + cookie.Value.Trim ()
			).ToArray ());
		}
	}
}
