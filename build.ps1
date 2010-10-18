$config = "Release"

function get-assembly-title
{
	param([string] $Path)

	$file = get-item $Path
	$content = [io.file]::ReadAllBytes($file.fullname)
	$a = [System.Reflection.Assembly]::Load($content)
	$d = [System.Attribute]::GetCustomAttribute($a, [System.Reflection.AssemblyTitleAttribute])

	return $d.Title
}

# tools
$msbuild = "$Env:SYSTEMROOT\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$zip = "$Env:ProgramFiles\7-Zip\7z.exe"

if ((test-path $zip) -eq $False)
{
	"Could not find 7-zip, exiting."
	return
}

$projects = @( "Enyim.Caching", "Membase" )
$includes = @{ "Enyim.Caching.Log4NetAdapter" = "log4net"; "Enyim.Caching.NLogAdapter" = "NLog" }

# remove the output folders
try {
	$projects | % { @("$_\bin", "$_\obj") } | where-object { test-path $_ } | remove-item -Recurse -Force -ErrorAction Stop
} catch {
	write-host "Couldn't remove the output directories, exiting." -foregroundcolor red
	return
}

# build the projects
.$msbuild /v:m /nologo /target:Rebuild /property:"Configuration=$config;IsReleaseBuild=true" Enyim.Caching.sln

$projects | % { 

	$current = $_
	$destRoot = "$current\bin\$config\"

	$includes.Keys | % {

		$includeDest = $destRoot + "\" + $includes[$_]
		md $includeDest

		$what = @("$_\Bin\$config\$_.*", "$_\Bin\$config\Demo.config")
		copy $what -Destination $includeDest
	}

	# we have to remove the tag from the version (emc2.3.4-9786545)
	$version = get-assembly-title -Path "$current\bin\$config\$current.dll" 
        $zipname = "..\..\..\" + $current + "." + ($version -replace "^[^0-9]+", "") + ".zip"

	# 7zip roots the files relative to the current path
	pushd
	cd "$current\bin\release"

	ren "$current.dll.config" "Demo.config"

	.$zip a -mx9 "$zipname" "." "..\..\..\LICENSE" "..\..\..\CHANGES" "..\..\..\README.mdown"

	popd
}

## all done
