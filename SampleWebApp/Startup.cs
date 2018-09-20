using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.SampleWebApp.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Enyim.Caching.SampleWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEnyimMemcached();
            //services.AddEnyimMemcached(Configuration);
            //services.AddEnyimMemcached(Configuration, "enyimMemcached");
            //services.AddEnyimMemcached(Configuration.GetSection("enyimMemcached"));
            services.AddTransient<IBlogPostService, BlogPostService>();
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseEnyimMemcached();
            app.UseMvcWithDefaultRoute();
        }
    }
}
