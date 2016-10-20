using Enyim.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Builder
{
    public static class EnyimMemcachedApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseEnyimMemcached(this IApplicationBuilder app)
        {
            try
            {
                app.ApplicationServices.GetService<IMemcachedClient>()
                    .GetAsync<string>("EnyimMemcached").Wait();
                Console.WriteLine("EnyimMemcached Started.");
            } catch (Exception ex)
            {
                app.ApplicationServices.GetService<ILogger<IMemcachedClient>>()
                    .LogError(new EventId(), ex, "EnyimMemcached Failed.");
            }
            return app;
        }
    }
}
