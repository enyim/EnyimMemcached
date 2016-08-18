# apveyor compatibility
<#
enum ParallelOptions
{
	None
	Collections
	Assemblies
	All
}
#>

function Invoke-XUnit {
	Param (
		[Parameter(ValueFromPipeline, Mandatory)]
		[string[]]  $Assemblies,

		<# [ParallelOptions] $Parallel = [ParallelOptions]::None, #>
		[string] $Parallel = "None",
		[Hashtable] $Exclude = @{ },
		[Hashtable] $Include = @{ },
		[string[]] $Class,
		[string[]] $Method,
		[string[]] $Namespace,
		[switch] $DebugMode = $false
	)

	xunit.console $Assemblies -nologo `
				(hash-param "notrait" $Exclude) `
				(hash-param "trais" $Include) `
				(list-param "method" $Method) `
				(list-param "class" $Class) `
				(list-param "namespace" $Namespace) `
				(switch-param "debug" $DebugMode) `
				(switch-param "quiet" $quiet) `
				-parallel $Parallel.ToLowerInvariant()
}

Export-ModuleMember -function Invoke-XUnit

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
