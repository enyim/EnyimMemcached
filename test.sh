#!/usr/bin/env bash
dotnet test SampleWebApp.IntegrationTests/*.csproj -c Release
dotnet test Enyim.Caching.Tests/*.csproj -c Release 
dotnet test MemcachedTest/*.csproj -c Release
