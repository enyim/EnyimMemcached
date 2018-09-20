using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Enyim.Caching.SampleWebApp.Models;

namespace Enyim.Caching.SampleWebApp.Services
{
    public interface IBlogPostService
    {
        ValueTask<IEnumerable<BlogPost>> GetRecent(int itemCount);
    }
}
