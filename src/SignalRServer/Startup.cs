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
using SignalRServer.Benchmark;
using SignalRServer.Hubs;

namespace SignalRServer
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

            services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
            {
                builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithOrigins("http://localhost:8000");
            }));

            // ע�� SignalR ����
            var signalrBuilder = services.AddSignalR(config =>
            {
                // �Ƿ���ͻ��˷�����ϸ�Ĵ�����Ϣ����ϸ�Ĵ�����Ϣ�������Է������������쳣����ϸ��Ϣ��
                config.EnableDetailedErrors = true;
            });

            /*-------------------MessagePack Э��-----------------------*/


            /*
             * https://docs.microsoft.com/zh-cn/aspnet/core/signalr/messagepackhubprotocol?view=aspnetcore-3.1
             * 
             * 1. MessagePack��һ�ֿ��١�����Ķ��������л���ʽ�� �����ܺʹ�����Ҫ����ʱ���������ã���Ϊ���ᴴ����JSON��С����Ϣ�� 
             *    �ڲ鿴������ٺ���־ʱ�����ܶ�ȡ��������Ϣ��������Щ�ֽ���ͨ�� MessagePack ���������ݵġ� SignalR�ṩ�� MessagePack ��ʽ������֧�֣���Ϊ�ͻ��˺ͷ������ṩҪʹ�õ� Api��
             *    С�������ᱻ�����һ���ֽڣ��̵��ַ�������ֻ��Ҫ�����ĳ��ȶ�һ�ֽڵĴ�С��
             * 
             * 2. ǰ������ MesagePack Э��֧��
             *    yarn add @microsoft/signalr-protocol-msgpack
             *     const connection = new signalR.HubConnectionBuilder()
             *              .withUrl("/chathub")
             *              .withHubProtocol(new signalR.protocols.msgpack.MessagePackHubProtocol())
             *              .build();
             */

            //signalrBuilder.AddMessagePackProtocol(
            //// �Զ��� MessagePack ����������ݵĸ�ʽ��FormatterResolvers ������������ MessagePack ���л�ѡ��
            ////options =>
            ////{
            ////    options.FormatterResolvers = new List<MessagePack.IFormatterResolver>()
            ////    {
            ////        MessagePack.Resolvers.StandardResolver.Instance
            ////    };
            ////}
            //);

            services.AddSingleton<ConnectionCounter>();
            services.AddHostedService<HostedCounterService>();

            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();
            app.UseRouting();

            //�������п���cors����ConfigureServices���������õĿ����������
            //ע�⣺UseCors�������UseRouting��UseEndpoints֮��
            app.UseCors("CorsPolicy");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<ChatHub>("/hubs/chathub");
                endpoints.MapControllers();
            });
        }
    }
}
