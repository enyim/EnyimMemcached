using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Enyim.Caching.Memcached;
using Enyim.Caching.Configuration;
using Enyim.Caching;
using System.Threading;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MemcachedTest
{
	public class FailurePolicyTest
	{
		[Fact]
		public void TestIfCalled()
		{
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("memcached", 11212));
            services.AddLogging();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetService<IMemcachedClientConfiguration>();
            config.SocketPool.FailurePolicyFactory = new FakePolicy();
            config.SocketPool.ConnectionTimeout = TimeSpan.FromSeconds(1);
            config.SocketPool.ReceiveTimeout = TimeSpan.FromSeconds(1);

            var logger = serviceProvider.GetService<ILoggerFactory>();

            var client = new MemcachedClient(logger, config);            

            Assert.Null(client.Get("a"));
		}

		class FakePolicy : INodeFailurePolicy, INodeFailurePolicyFactory
		{
			bool INodeFailurePolicy.ShouldFail()
			{
				Assert.True(true);

				return true;
			}

			INodeFailurePolicy INodeFailurePolicyFactory.Create(IMemcachedNode node)
			{
				return new FakePolicy();
			}
		}

		[Fact]
		public void TestThrottlingFailurePolicy()
		{
            IServiceCollection services = new ServiceCollection();
            services.AddEnyimMemcached(options => options.AddServer("localhost", 11212));
            services.AddLogging();
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            var config = serviceProvider.GetService<IMemcachedClientConfiguration>();           
			config.SocketPool.FailurePolicyFactory = new ThrottlingFailurePolicyFactory(4, TimeSpan.FromMilliseconds(2000));
			config.SocketPool.ConnectionTimeout = TimeSpan.FromMilliseconds(5);
			config.SocketPool.ReceiveTimeout = TimeSpan.FromMilliseconds(5);
			config.SocketPool.MinPoolSize = 1;
			config.SocketPool.MaxPoolSize = 1;

            var logger = serviceProvider.GetService<ILoggerFactory>();
            var client = new MemcachedClient(logger, config);
            var canFail = false;
			var didFail = false;

			client.NodeFailed += node =>
			{
				Assert.True(canFail, "canfail");

				didFail = true;
			};

			Assert.Null(client.Get("a"));
			Assert.Null(client.Get("a"));

			canFail = true;
			Thread.Sleep(2000);

			Assert.Null(client.Get("a"));
			Assert.Null(client.Get("a"));
			Assert.Null(client.Get("a"));
			Assert.Null(client.Get("a"));

			Assert.True(didFail, "didfail");
		}
	}
}
