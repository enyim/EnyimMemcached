using Enyim.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;

namespace Microsoft.AspNetCore.Builder
{
    public static class EnyimMemcachedApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEnyimMemcached(this IApplicationBuilder app)
        {
            var logger = app.ApplicationServices.GetService<ILogger<IMemcachedClient>>();
            try
            {
                var client = app.ApplicationServices.GetRequiredService<IMemcachedClient>();
                client.GetValueAsync<string>("UseEnyimMemcached").Wait();
                Console.WriteLine("EnyimMemcached connected memcached servers.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed in UseEnyimMemcached");
            }

            return app;
        }
    }
}
