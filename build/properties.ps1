$buildParams =  @{
	"solution_name" = "Enyim.Caching.sln";
	"projects" = @( "Enyim.Caching", "Membase" );
	"extras" = @{ "Enyim.Caching.Log4NetAdapter" = "log4net"; "Enyim.Caching.NLogAdapter" = "NLog" };
	"packages" = @( "Enyim.Caching", "Enyim.Caching.Log4NetAdapter", "Enyim.Caching.NLogAdapter", "Membase" );
}
