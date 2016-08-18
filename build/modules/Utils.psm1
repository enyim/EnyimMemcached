$known_keys = @{
	"remote_key.snk" = $true;
	"enyim.snk" = $true;
	"private_key.snk" = $true;
	"enyim_public.snk" = $false;
}

function Find-SigningKey {
	Param (
		[Parameter(Mandatory)]
		[string] $SolutionRoot
	)

	if ($env:REMOTE_KEY) {
		[System.IO.File]::WriteAllBytes("$SolutionRoot\remote_key.snk", [System.Convert]::FromBase64String($env:REMOTE_KEY))
	}

	foreach ($kvp in $known_keys.GetEnumerator())
	{
		$key = "$SolutionRoot\$($kvp.Key)"
		if (test-path $key) {
			return @{ Path = $key; Private = $kvp.Value }
		}
	}
}

function Remove-SigningKey {
	Param (
		[Parameter(Mandatory)]
		[string] $SolutionRoot
	)

	$p = "$SolutionRoot\remote_key.snk"

	if ($env:REMOTE_KEY -and (test-path $p)) {
		Remove-Item $p -Force
	}
}

function Get-MSBuildSigningParameters {
	Param (
		[Parameter(Mandatory)]
		[string] $SolutionRoot
	)

	$p = Find-SigningKey $SolutionRoot

	if ($p) {
		return @{
			AssemblyOriginatorKeyFile = $p.Path;
			DelaySign = !$p.Private;
			SignAssembly = $true;
		}
	}

	return @{ SignAssembly = $false; }
}

Export-ModuleMember -function Get-MSBuildSigningParameters, Find-SigningKey, Remove-SigningKey

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
