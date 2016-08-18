$git_where = get-command git -CommandType Application -ErrorAction SilentlyContinue -totalcount 1

if ($git_where -eq $null) {
	$env:path = "C:\Program Files\Git\bin\;C:\Program Files (x86)\Git\bin\;$env:path"
}

function Get-ProjectVersion {
	Param(
		[Parameter(Mandatory)]
		[string] $Root
	)

	$c = ConvertFrom-Json $((GitVersion $root) -join "`n")
	$short =  get-short-commit-hash $c.Sha
	$semver = $c.SemVer
	$mmp = $c.MajorMinorPatch

	[ordered]@{
		SemVer = $semver;
		#FullSemver = "$semver+$( join-ne @($c.CommitsSinceVersionSource, $short) "." )"
		#Informal = "$semver+$( join-ne @($c.CommitsSinceVersionSource, $c.BranchName, $short) "." )";
		FullSemver = "$semver+$short"
		Informal = "$semver+$($c.BranchName).$short";
		NuGet = (join-ne "$mmp",($c.PreReleaseTag -replace '\.','') "-");

		Major = $c.Major;
		Minor = $c.Minor;
		Patch = $c.Patch;

		Version = $mmp;
		Assembly = ($c.Major, $c.Minor, 0, 0) -join "."
	}
}

function get-commit-hash ($ref) {
	(git log --pretty=format:"%H %h" -1 $ref 2> $null) -split " "
}

function get-long-commit-hash ($ref) {
	(get-commit-hash $ref)[0]
}

function get-short-commit-hash ($ref) {
	(get-commit-hash $ref)[1]
}

function join-ne($v, $s) { ($v | ? { $_ }) -join $s }

Export-ModuleMember -function Get-ProjectVersion

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

