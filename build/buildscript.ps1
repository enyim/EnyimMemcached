$build_dir = Split-Path $psake.build_script_file

$solution_dir = Resolve-Path "$build_dir\.."
$out_dir = (md "$solution_dir\output\" -force)
$solution_file = Resolve-Path "$solution_dir\Enyim.Caching.sln"

$package_push_urls = @{ "myget"="https://www.myget.org/F/enyimmemcached/api/v2/package"; "nuget"="https://www.nuget.org/api/v2/package"; }
$symbol_push_urls = @{ "myget"="https://www.myget.org/F/enyimmemcached/symbols/api/v2/package"; "nuget"=""; }

#
# build script
#
Properties {

	#used by build/package
	$configuration = "Release"
	$platform = "Any CPU"

	#used by push
	$push_target = "myget"
	$push_symbols = $false
	$push_key = $null
}

Task Default -depends Pack
Task Rebuild -depends Clean, Build -description "clean & build"

#
# CLEAN
#
#

Task Clean -description "Remove all files created by the build process" {

	do-build -target "Clean" -project $solution_file
}

#
# BUILD
#
#

Task Build -description "Build the projects" -depends _Restore {

	try
	{
		$v = Get-ProjectVersion $solution_dir
		$p = Get-MSBuildSigningParameters "$solution_dir"

		$p += @{
				AssemblyVersion = $v.Assembly;
				AssemblyInformalVersion = $v.Informal;
		}

		if ($env:APPVEYOR) {
			appveyor UpdateBuild -version "$($v.Informal).$env:APPVEYOR_BUILD_NUMBER"
		}

		do-build -target "Build" -project $solution_file -properties $p
	}
	finally
	{
		Remove-SigningKey "$solution_dir"
	}
}

#
# TEST
#
#

Task Test -description "Run the unit tests" -Depends Build {
	Exec {
		gci "*.tests/bin/release/*.tests.dll" -Recurse |
			Select-Object -ExpandProperty FullName |
			Invoke-NUnit
	}
}

#
# PACKAGE
#
#

Task Pack -description "Build the nuget packages" -Depends Test {

	$v = Get-ProjectVersion $solution_dir

	gci $solution_dir "*.nuspec" -Recurse | % {

		write-Host -ForegroundColor Green "`nCreating package for $_"

		$nuspec = $_.FullName

		Exec {
			Nuget-Pack $nuspec `
					-symbols `
					-properties @{ configuration=$configuration } `
					-version $v.NuGet `
					-output $out_dir
		}
	}
}

#
# PUSH
#
#

Task Push -description "Push the Nuget packages to `$push_target" -depends Pack {

	assert ($package_push_urls.ContainsKey($push_target)) "Invalid package host: $push_target"
	assert (![String]::IsNullOrWhiteSpace($push_key)) "Invalid or missing API key."

	$target = $package_push_urls[$push_target]
	$v = Get-ProjectVersion $solution_dir

	(gci $out_dir "*$( $v.NuGet ).nupkg") | % {

		$package = $_.FullName

		Write-Host -ForegroundColor Green "`nPushing package $($_.Name) to '$push_target'"

		Exec { Nuget-Push $package -apikey $push_key -target $target }

		$symbols = [System.IO.Path]::ChangeExtension($_.FullName, ".symbols.nupkg")
		$symbol_target = $symbol_push_urls[$push_target]

		# empty symbol url means the target we are using do not support/need symbol packages
		# (e.g. nugetpushes symbols automatically)
		if ($symbol_target -and (test-path $symbols)) {
			Write-Host "  Pushing symbol package $( [System.IO.Path]::GetFileName($symbols) ) to '$push_target'"
			Exec { Nuget-Push $symbols -apikey $push_key -target ($symbol_push_urls[$push_target]) }
		}
	}
}

#
# NUGET RESTORE
#
#

Task _Restore -description "Restore the Nuget packages" {
	Exec { nuget restore $solution_file }
}

#
# UTILS
#
#

function bootstrap {

	set-location $solution_dir

	# install all tools we are using (x/nunit, etc)
	[string] $packages_dir = (md "$solution_dir\packages" -force)
	Exec { & "$build_dir\nuget" restore "$build_dir\packages.config" -PackagesDirectory $packages_dir }

	# add all tools to PATH
	$packages_config = [xml](gc "$build_dir\packages.config")
	$extras = $packages_config.packages.package | % { resolve-path "$packages_dir\$( $_.id ).$( $_.version )\tools" }
	$extras = $extras -join ";"
	$env:Path = "$build_dir;$extras;$env:Path"
}

function do-build($target, $project, $properties) {

	Assert $configuration "Configuration must be specified"
	Assert $platform "Platform must be specified"

	Exec { $project | Invoke-MSBuild -Target $target `
									-Configuration $configuration `
									-Platform $platform `
									-Properties $properties
	}
}

#
# install tools, configure search path
#
#

bootstrap

<#

	Copyright (c) Attila Kiskó, enyim.com

	Licensed under the Apache License, Version 2.0 (the "License");
	you may not use this file except in compliance with the License.
	You may obtain a copy of the License at

		http://www.apache.org/licenses/LICENSE-2.0

	Unless required by applicable law or agreed to in writing, software
	distributed under the License is distributed on an "AS IS" BASIS,
	WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	See the License for the specific language governing permissions and
	limitations under the License.

#>
