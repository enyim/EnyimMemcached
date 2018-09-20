using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching.SampleWebApp.Models;

namespace Enyim.Caching.SampleWebApp.Services
{
    public class BlogPostService : IBlogPostService
    {
        public async ValueTask<IEnumerable<BlogPost>> GetRecent(int itemCount)
        {
            return new List<BlogPost> { new BlogPost { Title = "Hello World", Body = "EnyimCachingCore" } };
        }
    }
}
