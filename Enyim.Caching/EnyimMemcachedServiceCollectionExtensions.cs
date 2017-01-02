using Enyim.Caching;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EnyimMemcachedServiceCollectionExtensions
    {
        public static IServiceCollection AddEnyimMemcached(this IServiceCollection services, Action<MemcachedClientOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);
            services.Add(ServiceDescriptor.Transient<IMemcachedClientConfiguration, MemcachedClientConfiguration>());
            services.Add(ServiceDescriptor.Singleton<IMemcachedClient, MemcachedClient>());

            return services;
        }

        public static IServiceCollection AddDistributedEnyimMemcached(this IServiceCollection services, Action<MemcachedClientOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);
            services.Add(ServiceDescriptor.Transient<IMemcachedClientConfiguration, MemcachedClientConfiguration>());
            services.Add(ServiceDescriptor.Singleton<IDistributedCache, MemcachedClient>());

            return services;
        }
    }
}
