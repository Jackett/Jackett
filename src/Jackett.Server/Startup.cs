using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Server.Middleware;
using Jackett.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Text;
#if !NET461
using Microsoft.Extensions.Hosting;
#endif

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
                            options.LoginPath = new PathString(Helper.ServerService.BasePath() + "/UI/Login");
                            options.AccessDeniedPath = new PathString(Helper.ServerService.BasePath() + "/UI/Login");
                            options.LogoutPath = new PathString(Helper.ServerService.BasePath() + "/UI/Logout");
                            options.Cookie.Name = "Jackett";
                            options.Cookie.SameSite = SameSiteMode.None;
                        });



#if NET461
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
                    });
#else
            services.AddControllers(config =>
                    {
                        var policy = new AuthorizationPolicyBuilder()
                                            .RequireAuthenticatedUser()
                                            .Build();
                        config.Filters.Add(new AuthorizeFilter(policy));
                    })
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver(); //Web app uses Pascal Case JSON
                    });
#endif

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
            builder.RegisterType<SecuityService>().As<ISecuityService>().SingleInstance();
            builder.RegisterType<ServerService>().As<IServerService>().SingleInstance();
            builder.RegisterType<ProtectionService>().As<IProtectionService>().SingleInstance();
            builder.RegisterType<ServiceConfigService>().As<IServiceConfigService>().SingleInstance();
            builder.RegisterType<FilePermissionService>().As<IFilePermissionService>().SingleInstance();

            IContainer container = builder.Build();
            Helper.ApplicationContainer = container;

            Helper.Logger.Debug("Autofac container built");

            Helper.Initialize();

            return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#if NET461
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            Helper.applicationLifetime = applicationLifetime;
            app.UseResponseCompression();

            app.UseDeveloperExceptionPage();

            app.UseCustomExceptionHandler();

            string serverBasePath = Helper.ServerService.BasePath() ?? string.Empty;

            if (!string.IsNullOrEmpty(serverBasePath))
            {
                app.UsePathBase(serverBasePath);
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                // When adjusting these pareamters make sure it's well tested with various environments
                // See https://github.com/Jackett/Jackett/issues/3517
                ForwardLimit = 10,
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            var rewriteOptions = new RewriteOptions()
                .AddRewrite(@"^torznab\/([\w-]*)", "api/v2.0/indexers/$1/results/torznab", skipRemainingRules: true) //legacy torznab route
                .AddRewrite(@"^potato\/([\w-]*)", "api/v2.0/indexers/$1/results/potato", skipRemainingRules: true) //legacy potato route
                .Add(RedirectRules.RedirectToDashboard);

            app.UseRewriter(rewriteOptions);

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseMvc();
        }
#else
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStopping.Register(OnShutdown);
            Helper.applicationLifetime = applicationLifetime;
            app.UseResponseCompression();

            app.UseDeveloperExceptionPage();

            app.UseCustomExceptionHandler();

            string serverBasePath = Helper.ServerService.BasePath() ?? string.Empty;

            if (!string.IsNullOrEmpty(serverBasePath))
            {
                app.UsePathBase(serverBasePath);
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                // When adjusting these pareamters make sure it's well tested with various environments
                // See https://github.com/Jackett/Jackett/issues/3517
                ForwardLimit = 10,
                ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
            });

            var rewriteOptions = new RewriteOptions()
                .AddRewrite(@"^torznab\/([\w-]*)", "api/v2.0/indexers/$1/results/torznab", skipRemainingRules: true) //legacy torznab route
                .AddRewrite(@"^potato\/([\w-]*)", "api/v2.0/indexers/$1/results/potato", skipRemainingRules: true) //legacy potato route
                .Add(RedirectRules.RedirectToDashboard);

            app.UseRewriter(rewriteOptions);

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
#endif



        private void OnShutdown()
        {
            //this code is called when the application stops
        }
    }
}
