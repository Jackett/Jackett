using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Jackett.Server.Middleware;
using Jackett.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Text;

namespace Jackett.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.LoginPath = new PathString("/UI/Login");
                            options.AccessDeniedPath = new PathString("/UI/Login");
                            options.LogoutPath = new PathString("/UI/Logout");
                            options.Cookie.Name = "Jackett";
                        });

            services.AddMvc(config =>
                    {
                        var policy = new AuthorizationPolicyBuilder()
                                            .RequireAuthenticatedUser()
                                            .Build();
                        config.Filters.Add(new AuthorizeFilter(policy));
                    })
                    .AddJsonOptions(options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver(); //Web app uses Pascal Case JSON
                    })
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            RuntimeSettings runtimeSettings = new RuntimeSettings();
            Configuration.GetSection("RuntimeSettings").Bind(runtimeSettings);

            DirectoryInfo dataProtectionFolder = new DirectoryInfo(Path.Combine(runtimeSettings.DataFolder, "DataProtection"));

            services.AddDataProtection()
                        .PersistKeysToFileSystem(dataProtectionFolder)
                        .SetApplicationName("Jackett");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = new ContainerBuilder();

            Helper.SetupLogging(builder);

            builder.Populate(services);
            builder.RegisterModule(new JackettModule(runtimeSettings));
            builder.RegisterType<SecuityService>().As<ISecuityService>();
            builder.RegisterType<ServerService>().As<IServerService>();
            builder.RegisterType<ProtectionService>().As<IProtectionService>();
            builder.RegisterType<ServiceConfigService>().As<IServiceConfigService>();
            if (runtimeSettings.ClientOverride == "httpclientnetcore")
                builder.RegisterType<HttpWebClientNetCore>().As<WebClient>();

            IContainer container = builder.Build();
            Helper.ApplicationContainer = container;

            Helper.Initialize();

            return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            Helper.applicationLifetime = applicationLifetime;
            app.UseResponseCompression();

            app.UseDeveloperExceptionPage();

            app.UseCustomExceptionHandler();

            var rewriteOptions = new RewriteOptions()
                .Add(RewriteRules.RewriteBasePath)
                .AddRewrite(@"^torznab\/([\w-]*)", "api/v2.0/indexers/$1/results/torznab", skipRemainingRules: true) //legacy torznab route
                .AddRewrite(@"^potato\/([\w-]*)", "api/v2.0/indexers/$1/results/potato", skipRemainingRules: true) //legacy potato route
                .Add(RedirectRules.RedirectToDashboard);

            app.UseRewriter(rewriteOptions);

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(Helper.ConfigService.GetContentFolder()),
                RequestPath = "",
                EnableDefaultFiles = true,
                EnableDirectoryBrowsing = false
            });

            app.UseAuthentication();

            app.UseMvc();
        }

        private void OnShutdown()
        {
            //this code is called when the application stops
        }
    }
}
