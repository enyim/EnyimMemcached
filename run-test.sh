#!/usr/bin/env bash
dotnet test Enyim.Caching.Tests/Enyim.Caching.Tests.csproj -c Release
dotnet test MemcachedTest/MemcachedTest.csproj -c Release
