using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RemoteManagerBackend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Diagnostics;

namespace RemoteManagerBackend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DataContext>(x=>x.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
            services.AddSingleton<IFileProvider>(
            new PhysicalFileProvider(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")));
            services.AddControllers();
            services.AddCors();
            services.AddMvc().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            //////////////////////////////////////////////////////////

            app.UseWebSockets();
            app.Use(async (ctx, nextMsg) =>
            {
                Console.WriteLine("Web Socket is listening");
                if (ctx.Request.Path == "/nokya")
                {
                    if (ctx.WebSockets.IsWebSocketRequest)
                    {
                        var wSocket = await ctx.WebSockets.AcceptWebSocketAsync();
                        await Talk(ctx, wSocket);
                    }
                    else
                    {
                        ctx.Response.StatusCode = 400;
                    }
                }
                else
                {
                    await nextMsg();
                }
            });

            app.UseFileServer();


            //////////////////////////////////////////////////////////




            app.UseCors(x=>x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
            
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private async Task Talk(HttpContext hContext, WebSocket wSocket)
        {
            var bag = new byte[1024];
            var result = await wSocket.ReceiveAsync(new ArraySegment<byte>(bag), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var incomingMessage = System.Text.Encoding.UTF8.GetString(bag, 0, result.Count);
                Debug.WriteLine("\nClient says that '{0}'\n", incomingMessage);
                var rnd = new Random();
                var number = rnd.Next(1, 100);
                string message = string.Format("Your lucky Number is '{0}'. Don't remember that :)", number.ToString());
                byte[] outgoingMessage = System.Text.Encoding.UTF8.GetBytes(message);
                await wSocket.SendAsync(new ArraySegment<byte>(outgoingMessage, 0, outgoingMessage.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
                result = await wSocket.ReceiveAsync(new ArraySegment<byte>(bag), CancellationToken.None);
            }
            await wSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
