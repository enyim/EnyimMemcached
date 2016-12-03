using Enyim.Caching;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DemonConsoleApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("memcached", 11211));
            services.AddLogging();
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            var memcached = serviceProvider.GetService<IMemcachedClient>();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            loggerFactory.AddConsole(LogLevel.Debug);
            memcached.AddAsync("test", "Hello, World", 60).Wait();
            Console.WriteLine(memcached.GetAsync<string>("test").Result.Value);
            Console.Read();
        }
    }
}
