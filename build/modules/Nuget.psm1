function Nuget-Pack {
	Param (
		[Parameter(Mandatory)]
		[string] $Nuspec,

		[Alias("p")] [hashtable] $Properties = @{ },

		[string] $Output,
		[string] $BasePath,
		[switch] $Symbols,

		[Parameter(Mandatory)]
		[string] $Version
	)

	nuget pack $Nuspec (string-param "basepath" $(Split-Path $Nuspec)) `
							(string-param "outputdirectory" $Output) `
							(string-param "version" $Version) `
							(hash-param "properties" $Properties -join ";") `
							(switch-param "symbols" $Symbols) (_nuget_args)
}

function Nuget-Push {
	Param (
		[Parameter(Mandatory)]
		[string] $Package,
		[Parameter(Mandatory)]
		[string] $ApiKey,
		[string] $Target
	)

	nuget push $Package (string-param "Source" $Target) (string-param "ApiKey" $ApiKey) (_nuget_args)
}

function _nuget_args {
	"-Verbosity", "n"
}

export-modulemember -function Nuget-Pack, Nuget-Push

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

