Properties {
	# these options must be overriden by the parent script
	#$solution_name = "Enyim.Caching.sln"
	#$projects = @( "Enyim.Caching", "Membase" )
	#$packages = @( "Enyim.Caching", "Enyim.Caching.Log4NetAdapter", "Enyim.Caching.NLogAdapter" )
	#$extras = @{ "Enyim.Caching.Log4NetAdapter" = "log4net"; "Enyim.Caching.NLogAdapter" = "NLog" }

	# these are fixed
	$build_root = Split-Path $psake.build_script_file
	$source_root = resolve-path "$build_root\.."
	$output_root = join-path $build_root "output"
	$temp_root = join-path $build_root "_temp"
}

Include "./utils.ps1"
FormatTaskName (("-"*20) + "[{0}]" + ("-"*20))

Task Default -depends _NeedsPrivateKey, Rebuild, _Zip, _Nuget

Task Clean -depends _Clean {
	# removes the files created by the build process
}

Task Build -depends _Build {
	# builds the projects
}

Task Rebuild -depends Clean, Build {
	# clean & build
}

Task Nuget -depends _Nuget {
	# builds the nuget packages
}

################################################################################
################################################################################

Task _CheckConfig {
	Assert ($solution_name -or $false) "Solution file name must be specifed!"
	Assert (($projects -ne $null) -and ($projects.length -gt 0)) "Project(s) to be packaged must be specified!"
}

Task _NeedsPrivateKey {
	Assert ( $private_key_path -or $private_key_name ) "Either the key path or the key container name must be specified!"

	if ($private_key_path) {
		Assert (test-path $private_key_path) "The key file '$private_key_path' does not exist."
		$script:private_key_path = resolve-path $private_key_path
	}
}

#################### Clean ####################

Task _Clean -depends _CheckConfig {
	
	Write-Host "Cleaning the solution." -ForegroundColor Green

	Exec { msbuild "$source_root\$solution_name" /t:Clean /p:"Configuration=Release" }
}

#################### Build ####################

Task _Build -depends _CheckConfig {

	Write-Host "Building the solution." -ForegroundColor Green
		"got it: $script:private_key_path"

	Exec { msbuild "$source_root\$solution_name" /t:Build /p:"Configuration=Release;PrivateKeyName=$private_key_name;PrivateKeyPath=$script:private_key_path" }
}

#################### Nuget ####################

Task _Nuget -depends _Build -PreAction { create-output-dir } {

	$packages | % { 

		$package = $_

		$version = (get-assembly-version -Path "$source_root\$package\bin\Release\$package.dll")
		$version = "$( $version.Major ).$( $version.Minor )"

		Exec { ./tools/nuget pack "$source_root\$package\$package.nuspec" -Properties version=$version -OutputDirectory $output_root }
	}
}

#################### Zip ####################

Task _Zip -PreAction {
	if (test-path $temp_root) {
		remove-item $temp_root -Recurse -Force -ErrorAction Stop
	}

	mkdir $temp_root -ErrorAction Stop | out-null
	create-output-dir
} -PostAction {
	remove-item "$temp_root" -Recurse -Force -ErrorAction SilentlyContinue
} -depends _CheckConfig {

	Assert (test-path $temp_root) "temp_root missing"

	$zip = get7zip

	set-content "$temp_root\Readme.html" `
		-Value (transform-markdown `
			-TemplatePath "$build_root\template.html" `
			-FilePath "$source_root\README.mdown" `
			-Title "Read Me")

	$projects | % { 

		$proj = $_

		# temp\project_name
		$proj_dest = "$temp_root\$proj"

		mkdir $proj_dest | out-null
		copy "$source_root\$proj\bin\Release\*.*" $proj_dest
		copy "$source_root\$proj\*.config" $proj_dest

		set-content "$proj_dest\Changes.html" `
			-Value (transform-markdown `
						-TemplatePath "$build_root\template.html" `
						-FilePath "$source_root\$proj\Changes.mdown" `
						-Title "Changes")

		if ($extras -ne $null) {
			$extras.Keys | % {

				# temp\project_name\extra_project
				$extra_dest = $proj_dest + "\" + $extras[$_]
				md $extra_dest | out-null

				copy @("$source_root\$_\bin\release\$_.*", "$source_root\$_\Demo.config") -Destination $extra_dest
			}
		}

		# we have to remove the tag from the version (emc2.3.4-9786545)
		$version = get-assembly-title -Path "$proj_dest\$proj.dll" 
		$zipname = $output_root + "\" + $proj + "." + ($version -replace "^[^0-9]+", "") + ".zip"

		del $zipname -ErrorAction SilentlyContinue | out-null

		# 7zip roots the files relative to the current path
		pushd
		cd $proj_dest > $nul

		.$zip a -mx9 "$zipname" "." "$source_root\LICENSE" "$temp_root\Readme.html"

		popd
	}
}

#################### helpers ####################

function create-output-dir
{
	mkdir $output_root -ErrorAction SilentlyContinue | out-null
}

#################### EOF ####################