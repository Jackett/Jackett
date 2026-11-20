using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services;
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
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Serialization;
#if !NET471
using Microsoft.Extensions.Hosting;
#endif

namespace Jackett.Server
{
    public class Startup
    {
        private const string AllowAllOrigins = "AllowAllOrigins";

        private readonly IConfiguration _configuration;
        private readonly RuntimeSettings _runtimeSettings;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            _runtimeSettings = new RuntimeSettings();
            configuration.GetSection("RuntimeSettings").Bind(_runtimeSettings);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
#if NET471
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression()
                    .AddCors(
                        options =>
                        {
                            options.AddPolicy(name: AllowAllOrigins, corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin());

                        })
                    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.LoginPath = new PathString("/UI/Login");
                            options.AccessDeniedPath = new PathString("/UI/Login");
                            options.LogoutPath = new PathString("/UI/Logout");
                            options.Cookie.Name = "Jackett";
                        });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // When adjusting these parameters make sure it's well tested with various environments
                // See https://github.com/Jackett/Jackett/issues/3517
                options.ForwardLimit = 10;
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("fc00::"), 7));
                options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("fe80::"), 10));
            });

            services.AddMvc(
                        config => config.Filters.Add(
                            new AuthorizeFilter(
                                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())))
                    .AddJsonOptions(options => options.SerializerSettings.ContractResolver =
                                        new DefaultContractResolver()); //Web app uses Pascal Case JSON);

            var dataProtectionFolder = new DirectoryInfo(Path.Combine(_runtimeSettings.DataFolder, "DataProtection"));

            services.AddDataProtection()
                        .PersistKeysToFileSystem(dataProtectionFolder)
                        .SetApplicationName("Jackett");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = new ContainerBuilder();

            Helper.SetupLogging(builder);

            builder.Populate(services);
            builder.RegisterModule(new JackettModule(_runtimeSettings));
            builder.RegisterType<SecurityService>().As<ISecurityService>().SingleInstance();
            builder.RegisterType<ServerService>().As<IServerService>().SingleInstance();
            builder.RegisterType<ProtectionService>().As<IProtectionService>().SingleInstance();
            builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
            builder.RegisterType<ServiceConfigService>().As<IServiceConfigService>().SingleInstance();
            builder.RegisterType<FilePermissionService>().As<IFilePermissionService>().SingleInstance();

            var container = builder.Build();
            Helper.ApplicationContainer = container;

            Helper.Logger.Debug("Autofac container built");

            Helper.Initialize();

            return new AutofacServiceProvider(container);
        }
# else
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddResponseCompression()
                    .AddCors(
                        options =>
                        {
                            options.AddPolicy(name: AllowAllOrigins, corsPolicyBuilder => corsPolicyBuilder.AllowAnyOrigin());

                        })
                    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
                        options =>
                        {
                            options.LoginPath = new PathString("/UI/Login");
                            options.AccessDeniedPath = new PathString("/UI/Login");
                            options.LogoutPath = new PathString("/UI/Logout");
                            options.Cookie.Name = "Jackett";
                        });

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // When adjusting these parameters make sure it's well tested with various environments
                // See https://github.com/Jackett/Jackett/issues/3517
                options.ForwardLimit = 10;
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("fc00::"), 7));
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(IPAddress.Parse("fe80::"), 10));
            });

            services.AddControllers(
                        config => config.Filters.Add(
                            new AuthorizeFilter(
                                new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())))
                    .AddNewtonsoftJson(
                        options => options.SerializerSettings.ContractResolver = new DefaultContractResolver());

            var dataProtectionFolder = new DirectoryInfo(Path.Combine(_runtimeSettings.DataFolder, "DataProtection"));

            services.AddDataProtection()
                        .PersistKeysToFileSystem(dataProtectionFolder)
                        .SetApplicationName("Jackett");

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            Helper.SetupLogging(builder);

            builder.RegisterModule(new JackettModule(_runtimeSettings));
            builder.RegisterType<SecurityService>().As<ISecurityService>().SingleInstance();
            builder.RegisterType<ServerService>().As<IServerService>().SingleInstance();
            builder.RegisterType<ProtectionService>().As<IProtectionService>().SingleInstance();
            builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
            builder.RegisterType<ServiceConfigService>().As<IServiceConfigService>().SingleInstance();
            builder.RegisterType<FilePermissionService>().As<IFilePermissionService>().SingleInstance();
        }
#endif

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
#if NET471
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStarted.Register(OnStarted);
            applicationLifetime.ApplicationStopped.Register(OnStopped);
            Helper.applicationLifetime = applicationLifetime;
            app.UseResponseCompression();

            app.UseDeveloperExceptionPage();

            app.UseCustomExceptionHandler();

            var serverBasePath = Helper.ServerService.BasePath() ?? string.Empty;

            if (!string.IsNullOrEmpty(serverBasePath))
            {
                app.UsePathBase(serverBasePath);
            }

            app.UseForwardedHeaders();

            var rewriteOptions = new RewriteOptions()
                .AddRewrite(@"^torznab\/([\w-]*)", "api/v2.0/indexers/$1/results/torznab", skipRemainingRules: true) //legacy torznab route
                .AddRewrite(@"^potato\/([\w-]*)", "api/v2.0/indexers/$1/results/potato", skipRemainingRules: true) //legacy potato route
                .Add(RedirectRules.RedirectToDashboard);

            app.UseRewriter(rewriteOptions);

            app.UseStaticFiles();

            app.UseAuthentication();

            if (Helper.ServerConfiguration.AllowCORS)
                app.UseCors(AllowAllOrigins);

            app.UseMvc();
        }
#else
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            applicationLifetime.ApplicationStarted.Register(OnStarted);
            applicationLifetime.ApplicationStopped.Register(OnStopped);
            Helper.applicationLifetime = applicationLifetime;
            app.UseResponseCompression();

            app.UseDeveloperExceptionPage();

            app.UseCustomExceptionHandler();

            var serverBasePath = Helper.ServerService.BasePath() ?? string.Empty;

            if (!string.IsNullOrEmpty(serverBasePath))
            {
                app.UsePathBase(serverBasePath);
            }

            app.UseForwardedHeaders();

            var rewriteOptions = new RewriteOptions()
                .AddRewrite(@"^torznab\/([\w-]*)", "api/v2.0/indexers/$1/results/torznab", skipRemainingRules: true) //legacy torznab route
                .AddRewrite(@"^potato\/([\w-]*)", "api/v2.0/indexers/$1/results/potato", skipRemainingRules: true) //legacy potato route
                .Add(RedirectRules.RedirectToDashboard);

            app.UseRewriter(rewriteOptions);

            app.UseStaticFiles();

            app.UseAuthentication();

            app.UseRouting();

            if (Helper.ServerConfiguration.AllowCORS)
                app.UseCors(AllowAllOrigins);

            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }
#endif

        private static void OnStarted()
        {
            var elapsed = (DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds;
            Helper.Logger.Info($"Jackett startup finished in {elapsed:0.000} s");
        }

        private static void OnStopped() => Helper.Logger.Info("Jackett stopped");
    }
}
