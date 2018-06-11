using Enyim.Caching;
using Enyim.Caching.Configuration;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class EnyimMemcachedServiceCollectionExtensions
    {
        /// <summary>
        /// Add EnyimMemcached to the specified <see cref="IServiceCollection"/>.
        /// Read configuration via IConfiguration.GetSection("enyimMemcached")
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddEnyimMemcached(this IServiceCollection services)
        {
            return AddEnyimMemcachedInternal(services, null);
        }

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

            return AddEnyimMemcachedInternal(services, s => s.Configure(setupAction));
        }

        public static IServiceCollection AddEnyimMemcached(this IServiceCollection services, IConfigurationSection configurationSection)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configurationSection == null)
            {
                throw new ArgumentNullException(nameof(configurationSection));
            }

            if(!configurationSection.Exists())
            {
                throw new ArgumentNullException($"{configurationSection.Key} in appsettings.json");
            }

            return AddEnyimMemcachedInternal(services, s => s.Configure<MemcachedClientOptions>(configurationSection));
        }

        public static IServiceCollection AddEnyimMemcached(this IServiceCollection services, IConfiguration configuration, string sectionKey = "enyimMemcached")
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var section = configuration.GetSection(sectionKey);
            if (!section.Exists())
            {
                throw new ArgumentNullException($"{sectionKey} in appsettings.json");
            }

            return AddEnyimMemcachedInternal(services, s => s.Configure<MemcachedClientOptions>(section));
        }

        private static IServiceCollection AddEnyimMemcachedInternal(IServiceCollection services, Action<IServiceCollection> configure)
        {
            services.AddOptions();
            configure?.Invoke(services);

            services.TryAddSingleton<ITranscoder, DefaultTranscoder>();
            services.TryAddSingleton<IMemcachedKeyTransformer, DefaultKeyTransformer>();
            services.TryAddTransient<IMemcachedClientConfiguration, MemcachedClientConfiguration>();
            services.AddSingleton<MemcachedClient, MemcachedClient>();

            services.AddSingleton<IMemcachedClient>(factory => factory.GetService<MemcachedClient>());
            services.AddSingleton<IDistributedCache>(factory => factory.GetService<MemcachedClient>());

            return services;
        }        
    }
}
