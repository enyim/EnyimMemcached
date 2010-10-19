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

function transform
{
	param($Markdown, $TemplatePath, $FilePath, $Title)

	return (get-content $TemplatePath) -replace '\$title', $Title -replace '\$content', $Markdown.Transform([io.file]::ReadAllText($FilePath))
}

# prepare
pushd

# tools
$msbuild = "$Env:SYSTEMROOT\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
$zip = "$Env:ProgramFiles\7-Zip\7z.exe"

if ((test-path $zip) -eq $False)
{
	"Could not find 7-zip, exiting."
	return
}

if ((test-path "build.ps1") -eq $True) {
	cd ..
}

$projectRoot = (get-location).path

$buildRoot = "$projectRoot\build\output"

$projects = @( "Enyim.Caching", "Membase" )
$includes = @{ "Enyim.Caching.Log4NetAdapter" = "log4net"; "Enyim.Caching.NLogAdapter" = "NLog" }

# remove the output folders

try {
	if ((test-path $buildRoot) -eq $True) { 
		remove-item $buildRoot -Recurse -Force -ErrorAction Stop
	}
} 
catch {
	write-host "Couldn't clean the build directory, exiting." -foregroundcolor red
	return
}

md $buildRoot > $nul

[System.Reflection.Assembly]::Load([io.file]::ReadAllBytes("$projectRoot\build\markdownsharp.dll")) > $nul
$md = new-object MarkdownSharp.Markdown

set-content "$buildRoot\Readme.html" -Value (transform -TemplatePath "$projectRoot\build\template.html" -FilePath "$projectRoot\README.mdown" -Title "Read Me" -Markdown $md)

# build the projects
.$msbuild /m:1 /v:m /nologo /target:Rebuild /property:"Configuration=Release;IsReleaseBuild=true" Enyim.Caching.sln

$projects | % { 

	$current = $_
	$currentDest = "$buildRoot\$current"

	$includes.Keys | % {

		$includeDest = $currentDest + "\" + $includes[$_]
		md $includeDest > $nul

		$what = @("$buildRoot\$_\$_.*", "$buildRoot\$_\Demo.config")
		copy $what -Destination $includeDest
	}

	set-content "$currentDest\Changes.html" -Value (transform -TemplatePath "$projectRoot\build\template.html" -FilePath "$currentDest\Changes.mdown" -Title "Change Log" -Markdown $md)
	rm "$currentDest\Changes.mdown" > $nul

	# we have to remove the tag from the version (emc2.3.4-9786545)
	$version = get-assembly-title -Path "$currentDest\$current.dll" 
        $zipname = $projectRoot + "\" + $current + "." + ($version -replace "^[^0-9]+", "") + ".zip"

	del $zipname -ErrorAction SilentlyContinue

	# 7zip roots the files relative to the current path
	pushd
	cd $currentDest > $nul

	.$zip a -mx9 "$zipname" "." "$projectRoot\LICENSE" "$buildRoot\Readme.html"

	popd
}

## all done
popd