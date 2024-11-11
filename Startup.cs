using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SJPCORE.Models;
using SJPCORE.Middleware;
using MQTTnet.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using SJPCORE.Models.Attribute;
using System.Linq;
using SJPCORE.Util;
using SJPCORE.Models.Interface;
using System.Collections.Generic;

namespace SJPCORE
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public async void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
            });
            services.AddSignalR();
            services.AddControllersWithViews();
            
            services.AddTransient<DapperContext>();
            services.AddTransient<AppDbContext>();
            services.AddTransient<PassParam>();
            services.AddSingleton<DapperContext>();
            await DatabaseInitializer.InitializeDatabaseAsync(services.BuildServiceProvider().GetService<DapperContext>());
            services.AddSingleton<AppDbContext>();
            services.AddSingleton<PassParam>();

            services.AddTransient<AuthPassed>();
            services.AddSingleton<AuthPassed>();

            services.AddScoped<ISecretKeyHelper, SecretKeyHelper>();
            services.AddHostedService<PuppeteerBackgroundService>();
            services.AddSingleton<EMQXClientService>(); // Add as singleton
            services.AddHostedService(provider => provider.GetRequiredService<EMQXClientService>());
            
            SimpleCRUD.SetDialect(SimpleCRUD.Dialect.SQLite);

            services.AddMemoryCache();
            services.AddControllersWithViews();
            services.AddDistributedMemoryCache();

            services.AddConnections();

            services.AddHostedMqttServer(
                optionsBuilder =>
                {
                    optionsBuilder.WithDefaultEndpoint();
                });
            services.AddMqttConnectionHandler();
            services.AddSingleton<MQTTController>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MQTTController mqttController, DapperContext context)
        {
            
            GlobalParameter.Config = context.GetConfig();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            
            app.UseCors("CorsPolicy");
            app.UseAuthorization();
            app.UseMiddleware<Middleware.PassParam>();
            app.UseMiddleware<Middleware.AuthPassed>();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/chatHub");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=station}/{action=broadcast}/{id?}");
                
                endpoints.MapConnectionHandler<MqttConnectionHandler>(
                       "/mqtt",
                       httpConnectionDispatcherOptions => httpConnectionDispatcherOptions.WebSockets.SubProtocolSelector =
                           protocolList => protocolList.FirstOrDefault() ?? string.Empty);
            });
            app.UseMqttServer(
               server =>
               {
                   server.ClientAcknowledgedPublishPacketAsync += mqttController.HandleClientAcknowledgedPublishPacketAsync;
                   server.ApplicationMessageNotConsumedAsync += mqttController.HandleMessageNotConsumedAsync;
                   server.ValidatingConnectionAsync += mqttController.ValidateConnection;
                   server.ClientConnectedAsync += mqttController.OnClientConnected;
                   server.ClientDisconnectedAsync += mqttController.OnClientDisconnected;
                   server.ClientSubscribedTopicAsync += mqttController.OnClientSubscribedTopic;
                   server.ClientUnsubscribedTopicAsync += mqttController.OnClientUnsubscribedTopic;
               });
               
        }

        private System.Threading.Tasks.Task Server_InterceptingPublishAsync(MQTTnet.Server.InterceptingPublishEventArgs arg)
        {
            throw new NotImplementedException();
        }
    }
}
