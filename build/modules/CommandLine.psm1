function format-hashitem {
	Param (
		[Parameter(ValueFromPipeline)]
		$item,
		$format = "{0}={1}"
	)

	Process {
		($format -f $_.key, $_.value)
	}
}

function format-switch {
	Param (
		[Parameter(Mandatory, Position = 0)]
		[string]$name,
		[Parameter(ValueFromPipeline, Position = 1)]
		[string]$value,
		[alias("f")]
		[string]$format
	)

	Begin {
		if (!$format) { $format = "`-{0} " }
	}

	Process {
		$sw = $format -f $name

		if ($sw.EndsWith(" ")) {
			$sw= $sw.TrimEnd()

			if ($value) {
				return $sw, $value
			}
			else {
				return $sw
			}
		}

		return $sw + $value
	}
}

function hash-param {
	Param(
		[Parameter(Mandatory)]
		[string] $name,
		$hash,
		$join,
		$format
	)

	if ($hash) {
		if ($join -ne $null) {
			(($hash.GetEnumerator() | format-hashitem ) -join $join) | format-switch $name -f $format
		}
		else {
			$hash.GetEnumerator() | format-hashitem | format-switch $name -f $format
		}
	}
}

function list-param {
	Param(
		[Parameter(Mandatory)]
		[string] $name,
		[string[]]$list,
		$join,
		$format
	)

	if ($list) {
		if ($join) {
			($list -join $join) | format-switch $name -f $format
		} else {
			$list | format-switch $name -f $format
		}
	}
}

function switch-param {
	Param(
		[Parameter(Mandatory)]
		[string] $name,
		$when,
		$format = $null
	)

	if ($when) { format-switch $name $null -f $format }
}

function string-param {
	Param(
		[Parameter(Mandatory)]
		[string] $name,
		$value,
		$format = $null
	)

	if ($value) { format-switch $name $value -f $format }
}

Export-ModuleMember -function hash-param, list-param, switch-param, string-param

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
