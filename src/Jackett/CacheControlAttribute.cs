using System.Web.Http.Filters;

namespace Jackett
{
    public class JackettAPINoCacheAttribute : System.Web.Http.Filters.ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if(null!= actionExecutedContext && 
               null!= actionExecutedContext.Response && 
               null!= actionExecutedContext.Response.Headers)
            actionExecutedContext.Response.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue()
            {
                NoStore = true,
                Private = true
            };

            base.OnActionExecuted(actionExecutedContext);
        }
    }
}
