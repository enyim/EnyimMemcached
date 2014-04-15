..\.nuget\nuget pack ..\%1\%1.csproj -OutputDirectory packages -Build -Properties Configuration=Release
..\.nuget\nuget push packages\EnyimMemcached.*.nupkg -s http://nuget.cnitblog.com Aqwe!@34588ujmhsxd
pause 