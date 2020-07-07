using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.WebUtilities;

namespace Jackett.Server.ActionFilters
{
    public class DownloadActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            // in Torznab RSS feed the "link" and "enclosure url" attributes are encoded following the RSS specification
            // replacing & with &amp;
            // valid link => http://127.0.0.1:9117/dl/1337x/?jackett_apikey=ygm5k29&path=Q2ZESjhBYnJEQUIxeGd&file=Little+Mons
            // encoded  => http://127.0.0.1:9117/dl/1337x/?jackett_apikey=ygm5k29&amp;path=Q2ZESjhBYnJEQUIxeGd&amp;file=Little+Mons
            // all RSS readers are able to decode the url and show the user the valid link
            // some Jackett users are not decoding the url properly and this causes a 404 error in Jackett download
            // this ActionFilter tries to do the decoding as a fallback, the RSS feed we provide is valid!
            if (filterContext.ActionArguments.ContainsKey("path") || !filterContext.HttpContext.Request.QueryString.HasValue)
                return;

            // recover the original query string and parse it manually
            var qs = filterContext.HttpContext.Request.QueryString.Value;
            qs = qs.Replace("&amp;", "&");
            var parsedQs = QueryHelpers.ParseQuery(qs);
            if (parsedQs == null)
                return;

            // inject the arguments in the controller
            if (parsedQs.ContainsKey("path") && !filterContext.ActionArguments.ContainsKey("path"))
                filterContext.ActionArguments.Add("path", parsedQs["path"].ToString());
            if (parsedQs.ContainsKey("file") && !filterContext.ActionArguments.ContainsKey("file"))
                filterContext.ActionArguments.Add("file", parsedQs["file"].ToString());
        }
    }
}
