using Log4net.NetCore.Data;
using Log4net.NetCore.Lib.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using log4netConfiguration = Log4net.NetCore.Lib.Appenders.Configuration;

namespace Log4net.NetCore.Web
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
            services.AddDbContext<Log4netDBContext>(options => options.UseSqlServer(Configuration["Logging:ConnectionString"]));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, Log4netDBContext context, ILoggerFactory loggerFactory)
        {
            string connectionString = Configuration["Logging:ConnectionString"];
            string logFilePath = Configuration["Logging:LogFilePath"];

            loggerFactory.AddLog4Net(new[]
            {
                log4netConfiguration.CreateConsoleAppender(),
                log4netConfiguration.CreateRollingFileAppender(logFilePath),
                log4netConfiguration.CreateTraceAppender(),
                log4netConfiguration.CreateAdoNetAppender(connectionString)
            });

            DBInitializer.Initialize(context);

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            
            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
