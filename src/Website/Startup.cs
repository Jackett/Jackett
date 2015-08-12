using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Website.Startup))]
namespace Website
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
