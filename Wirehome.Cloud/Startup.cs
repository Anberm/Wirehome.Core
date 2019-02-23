﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using Wirehome.Cloud.Controllers;
using Wirehome.Cloud.Filters;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud;
using Wirehome.Core.HTTP.Controllers;

namespace Wirehome.Cloud
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Startup
    {
        // ReSharper disable once UnusedMember.Global
        public void ConfigureServices(IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<DeviceConnectorService>();
            services.AddSingleton<AuthorizationService>();
            services.AddSingleton<RepositoryService>();
            services.AddSingleton<CloudMessageFactory>();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o => 
            {
                o.LoginPath = "/Cloud/Account/Login";
                o.LogoutPath = "/Cloud/Account/Logout";
                o.Events.OnRedirectToLogin = context =>
                {
                    // This ensures that API calls are not forwarded to the login
                    // page. They will only return 401 instead.
                    if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == (int)HttpStatusCode.OK)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }

                    return Task.CompletedTask;
                };
            });

            services.AddMvc(config =>
            {
                config.Filters.Add(new DefaultExceptionFilter());
            })
            .ConfigureApplicationPartManager(config =>
            {
                config.FeatureProviders.Remove(config.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
                config.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(CloudController).Namespace));
            });

            ConfigureSwaggerServices(services);
        }

        // ReSharper disable once UnusedMember.Global
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            AuthorizationService authorizationService,
            DeviceConnectorService deviceConnectorService)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));
            if (env == null) throw new ArgumentNullException(nameof(env));
            if (authorizationService == null) throw new ArgumentNullException(nameof(authorizationService));
            if (deviceConnectorService == null) throw new ArgumentNullException(nameof(deviceConnectorService));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseAuthentication();
            app.UseStaticFiles();

            ConfigureMvc(app);
            ConfigureSwagger(app);
            ConfigureConnector(app, deviceConnectorService, authorizationService);
            ConfigureHttpReverseProxy(app, deviceConnectorService);
        }

        private static void ConfigureHttpReverseProxy(IApplicationBuilder app, DeviceConnectorService deviceConnectorService)
        {
            app.Run(deviceConnectorService.TryDispatchHttpRequestAsync);
        }

        private static void ConfigureSwagger(IApplicationBuilder app)
        {
            app.UseSwagger(o => o.RouteTemplate = "/api/{documentName}/swagger.json");

            app.UseSwaggerUI(o =>
            {
                o.RoutePrefix = "api";
                o.DocumentTitle = "Wirehome.Cloud.API";
                o.SwaggerEndpoint("/api/v1/swagger.json", "Wirehome.Cloud API v1");
            });
        }

        private static void ConfigureMvc(IApplicationBuilder app)
        {
            app.UseMvc(config =>
            {
                config.MapRoute("default", "cloud/{controller}/{action=Index}/{id?}", null, null, null);
            });
        }

        private static void ConfigureConnector(IApplicationBuilder app, DeviceConnectorService connectorService, AuthorizationService authorizationService)
        {
            app.Map("/Connectors", config =>
            {
                config.UseWebSockets(new WebSocketOptions
                {
                    KeepAliveInterval = TimeSpan.FromMinutes(2),
                    ReceiveBufferSize = 4096
                });

                config.Use(async (context, next) =>
                {
                    if (!context.WebSockets.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        return;
                    }

                    try
                    {
                        var authorizationContext = authorizationService.AuthorizeDevice(context);

                        var deviceSessionIdentifier = new DeviceSessionIdentifier(
                            authorizationContext.IdentityUid,
                            authorizationContext.ChannelUid);

                        using (var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
                        {
                            await connectorService.RunAsync(deviceSessionIdentifier, webSocket, context.RequestAborted).ConfigureAwait(false);
                            context.Abort();
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    }
                });
            });
        }

        private static void ConfigureSwaggerServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info
                {
                    Title = "Wirehome.Cloud API",
                    Version = "v1",
                    Description = "This is the public API for the Wirehome.Cloud service.",
                    License = new License
                    {
                        Name = "Apache-2.0",
                        Url = "https://github.com/chkr1011/Wirehome.Core/blob/master/LICENSE"
                    },
                    Contact = new Contact
                    {
                        Name = "Wirehome.Core",
                        Email = string.Empty,
                        Url = "https://github.com/chkr1011/Wirehome.Core"
                    },
                });
            });
        }
    }
}
