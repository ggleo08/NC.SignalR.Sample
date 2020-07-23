using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SignalRServer.Benchmark;
using SignalRServer.Feedback;
using SignalRServer.Handlers;
using SignalRServer.Hubs;
using SignalRServer.Models;
using SignalRServer.Storage;

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
                .SetIsOriginAllowed(origin => true);
                //.WithOrigins("http://localhost:8000");
            }));

            // ע�� SignalR ����
            var signalrBuilder = services.AddSignalR(config =>
            {
                // �Ƿ���ͻ��˷�����ϸ�Ĵ�����Ϣ����ϸ�Ĵ�����Ϣ�������Է������������쳣����ϸ��Ϣ��
                config.EnableDetailedErrors = true;
                // �ͻ��������30s��û��������������κ���Ϣ����ô�������������Ϊ�ͻ����Ѿ��Ͽ������ˣ�����ֵΪ KeepAliveInterval ֵ������ 
                config.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
                // ������δ��15s����ͻ��˷�����Ϣ����15s��ʱ����������Զ�ping�ͻ��ˣ��������Ӵ򿪵�״̬
                config.KeepAliveInterval = TimeSpan.FromSeconds(15);

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

            // ע������ͳ�ƺ�̨����
            services.AddHostedService<HostedCounterService>();

            // ע����Ϣ������������
            services.AddHostedService<FeedbackMonitorService>();

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
                endpoints.MapHub<ProxyHub>("/signalr");
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AutofacModele());
        }
    }

    public class AutofacModele : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // �ͻ�����Ϣ�洢 -- ��ʱ
            //context.Services.AddSingleton<ConnectionCounter>();
            builder.RegisterType<ConnectionCounter>().SingleInstance();

            // �ͻ�����Ϣ�洢 -- ��ʱ
            //context.Services.AddSingleton<ClientStorage>(); 
            builder.RegisterType<ClientStorage>().SingleInstance();

            // ��Ϣ��ʷ��¼
            //context.Services.AddSingleton<MessageHistory>();
            builder.RegisterType<MessageHistory>().SingleInstance();

            // ����ע���������
            var assembly = Assembly.GetExecutingAssembly();
            var handlers = assembly.GetTypes().Where(p => p.IsClass && typeof(ICommandHandler).IsAssignableFrom(p)).ToList();
            handlers.ForEach(t =>
            {
                var att = t.GetCustomAttribute<InjectNamedAttribute>();
                if (att != null)
                {
                    builder.RegisterType(t).Named<ICommandHandler>(att.Named).InstancePerLifetimeScope();
                }
            });
        }
    }
}
