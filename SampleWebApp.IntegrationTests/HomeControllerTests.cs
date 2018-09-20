using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.SampleWebApp;
using Enyim.Caching.SampleWebApp.Controllers;
using Enyim.Caching.SampleWebApp.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SampleWebApp.IntegrationTests
{
    public class HomeControllerTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;

        public HomeControllerTests(WebApplicationFactory<Startup> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HomeController_Index()
        {
            var httpClient = _factory.CreateClient();
            var response = await httpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var memcachedClient = _factory.Server.Host.Services.GetRequiredService<IMemcachedClient>();
            var posts = await memcachedClient.GetValueAsync<IEnumerable<BlogPost>>(HomeController.CacheKey);
            Assert.NotNull(posts);
            Assert.NotEmpty(posts.First().Title);

            await memcachedClient.RemoveAsync(HomeController.CacheKey);
        }
    }
}
