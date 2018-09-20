using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.SampleWebApp.Models;
using Enyim.Caching.SampleWebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Enyim.Caching.SampleWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemcachedClient _memcachedClient;
        private readonly IBlogPostService _blogPostService;
        private readonly ILogger _logger;
        public static readonly string CacheKey = "blogposts-recent";

        public HomeController(
            IMemcachedClient memcachedClient,
            IBlogPostService blogPostService,
            ILoggerFactory loggerFactory)
        {
            _memcachedClient = memcachedClient;
            _blogPostService = blogPostService;
            _logger = loggerFactory.CreateLogger<HomeController>();
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogDebug("Executing _memcachedClient.GetValueOrCreateAsync...");

            var cacheSeconds = 600;
            var posts = await _memcachedClient.GetValueOrCreateAsync(
                CacheKey,
                cacheSeconds,
                async () => await _blogPostService.GetRecent(10));

            _logger.LogDebug("Done _memcachedClient.GetValueOrCreateAsync");

            return Ok(posts);
        }
    }
}
