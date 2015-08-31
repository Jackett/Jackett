using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class OWINRewriter : OwinMiddleware
    {
        string from;
        string to;

        public OWINRewriter(OwinMiddleware next, string from, string to)
        : base(next)
        {
            this.from = from;
            this.to = to;
        }

        public override async Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.HasValue &&
                context.Request.Path.Value.StartsWith(from, StringComparison.InvariantCultureIgnoreCase))
            {
                context.Request.Path = new PathString(to);
            }

            await Next.Invoke(context);
        }
    }

    public static class FileServerExtensions
    {
        public static IAppBuilder Rewrite(this IAppBuilder builder, string from, string to)
        {
            builder.Use<OWINRewriter>(from, to);
            return builder;
        }
    }
}
