using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Enyim.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Distributed;

namespace SampleWebApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            IsDevelopment = env.IsDevelopment();
        }

        public IConfigurationRoot Configuration { get; set; }

        public bool IsDevelopment { get; set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEnyimMemcached(Configuration.GetSection("enyimMemcached"));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseEnyimMemcached();

            var memcachedClient = app.ApplicationServices.GetService<IMemcachedClient>();
            var distributedCache = app.ApplicationServices.GetService<IDistributedCache>();
            var logger = loggerFactory.CreateLogger<MemcachedClient>();

            app.Run(async (context) =>
            {
                var cacheKey = "sample_response";
                var distributedCaceKey = "distributed_" + cacheKey;
                await memcachedClient.AddAsync(cacheKey, $"Hello World from {nameof(memcachedClient)}!", 60);
                await distributedCache.SetStringAsync(distributedCaceKey,$"Hello World from {nameof(distributedCache)}!");
                var cacheResult = await memcachedClient.GetAsync<string>(cacheKey);
                if (cacheResult.Success)
                {
                    var distributedCacheValue = await distributedCache.GetStringAsync(distributedCaceKey);
                    await context.Response
                        .WriteAsync($"memcachedClient: {cacheResult.Value}\ndistributedCache: {distributedCacheValue}");
                    await memcachedClient.RemoveAsync(cacheKey);
                    await distributedCache.RemoveAsync(distributedCaceKey);
                    logger.LogDebug($"Hinted cache with '{cacheKey}' key");
                }
                else
                {
                    var message = $"Missed cache with '{cacheKey}' key";
                    await context.Response.WriteAsync(message);
                    logger.LogError(message);
                }
            });
        }
    }
}
