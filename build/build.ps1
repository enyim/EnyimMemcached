<#
	.SYNOPSIS
		This is the entry point of the build process.

	.DESCRIPTION
		By default the build script rebuilds, signs and zips the project,
		so you need to specify the signing key using either the KeyPath or
		the KeyName parameters.

		You can still build the project locally if you need so, but the
		resulting assemblies will be delay signed, so you need to disable
		verification using 'sn -Vr' (see the MSDN for more information).

	.PARAMETER TaskList
		The comma-separated list of task(s) to be run from the build script.
		The following task name can be passed to the build script:
		    - Clean
		    - Build
		    - Rebuild
		    - Nuget

		If omitted the 'Default' task will be run.

	.PARAMETER KeyPath
		Specify the full path of a .snk file which will be used to sign
		the resulting assemblies. The file must contain both the public
		and the private keys.

	.PARAMETER KeyName
		Specify the name of the key container which will be used to sign
		the resulting assemblies. The file must contain both the public
		and the private keys.

	.EXAMPLE
		C:\PS> .\build.ps1 Rebuild

	.EXAMPLE
		C:\PS> .\build.ps1 -KeyPath C:\keys\enyim.snk

#>

param(
	[string]$TaskList = "Default",
	[string]$KeyPath = $null,
	[string]$KeyName = $null)

. .\properties.ps1

$tmp = $buildParams + @{ "private_key_path" = "$KeyPath"; "private_key_name" = "$KeyName"; }

import-module .\psake.psm1
invoke-psake -buildFile .\default.ps1 -TaskList $TaskList -framework 4.0 -parameters $tmp
remove-module psake
