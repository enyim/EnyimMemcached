function Invoke-MSBuild {
	[CmdletBinding()]
	Param (
		[Parameter(ValueFromPipeline, Mandatory)]
		[string[]]  $Project,

		[Parameter(Mandatory)]
		[Alias("t")] [string[]] $Target,
		[Alias("p")] [hashtable] $Properties = @{ },

		[string] $Configuration,
		[string] $Platform
	)

	Begin {
		$props = @{ }

		if ($Configuration) { $props["Configuration"] = $Configuration }
		if ($Platform) { $props["Platform"] = $Platform }
		if ($Properties) { $props = $props + $Properties }
	}

	Process {
		msbuild /nologo /m:2 `
				(list-param "t" $Target -format "/{0}:" -join ";") `
				(hash-param "p" $props -format "/{0}:" -join ";") `
				(string-param "v" $v -format "/{0}:") `
				$(if ($env:AppVeyorCI) { "/logger:C:\Program^ Files\AppVeyor\BuildAgent\Appveyor.MSBuildLogger.dll" }) `
				/v:n $_
	}
}

Export-ModuleMember -function Invoke-MSBuild

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
