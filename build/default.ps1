Properties {
	# these options must be overriden by the parent script
	#$solution_name = "Enyim.Caching.sln"
	#$projects = @( "Enyim.Caching", "Membase" )
	#$extras = @{ "Enyim.Caching.Log4NetAdapter" = "log4net"; "Enyim.Caching.NLogAdapter" = "NLog" }

	# these are fixed
	$build_root = Split-Path $psake.build_script_file
	$source_root = "$build_root\.."
	$output_root = "$build_root\output"
}

Include "./utils.ps1"
FormatTaskName (("-"*20) + "[{0}]" + ("-"*20))

Task Default -depends _NeedsPrivateKey, Rebuild, _Package

Task Clean -depends _Clean {
	# removes the files created by the build process
}

Task Build -depends _Build {
	# builds the projects
}

Task Rebuild -depends Clean,Build {
	# clean & build
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

	remove-output-dir

	Exec { msbuild "$source_root\$solution_name" /t:Clean /p:"Configuration=Release" } 
}

#################### Build ####################

Task _Build -depends _CheckConfig {

	Write-Host "Building the solution." -ForegroundColor Green
		"got it: $script:private_key_path"

	Exec { msbuild "$source_root\$solution_name" /t:Build /p:"Configuration=Release;PrivateKeyName=$private_key_name;PrivateKeyPath=$script:private_key_path" }
}

#################### Package ####################

Task _Package -depends _PreparePackage -PostAction { remove-output-dir } {

	$zip = get7zip

	$projects | % { 

		$proj = $_
		$proj_dest = "$output_root\$proj"

		# we have to remove the tag from the version (emc2.3.4-9786545)
		$version = get-assembly-title -Path "$proj_dest\$proj.dll" 
		$zipname = $build_root + "\" + $proj + "." + ($version -replace "^[^0-9]+", "") + ".zip"

		del $zipname -ErrorAction SilentlyContinue | out-null

		# 7zip roots the files relative to the current path
		pushd
		cd $proj_dest > $nul

		.$zip a -mx9 "$zipname" "." "$source_root\LICENSE" "$output_root\Readme.html"

		popd
	}
}

Task _PreparePackage -depends _CheckConfig, _Build -PreAction { remove-output-dir;create-output-dir } {

	$projects | % { 

		$proj = $_

		# output\project_name
		$proj_dest = "$output_root\$proj"

		mkdir $proj_dest | out-null
		copy "$source_root\$proj\bin\Release\*.*" $proj_dest
		copy "$source_root\$proj\Demo.config" $proj_dest

		set-content "$proj_dest\Changes.html" `
			-Value (transform-markdown `
						-TemplatePath "$build_root\template.html" `
						-FilePath "$source_root\$proj\Changes.mdown" `
						-Title "Changes")

		if ($extras -ne $null) {
			$extras.Keys | % {

				# output\project_name\extra_project
				$extra_dest = $proj_dest + "\" + $extras[$_]
				md $extra_dest | out-null

				copy @("$source_root\$_\bin\release\$_.*", "$source_root\$_\Demo.config") -Destination $extra_dest
			}
		}
	}

	set-content "$output_root\Readme.html" `
		-Value (transform-markdown `
			-TemplatePath "$build_root\template.html" `
			-FilePath "$source_root\README.mdown" `
			-Title "Read Me")
}

#################### helpers ####################

function remove-output-dir
{
	remove-item $output_root -Recurse -Force -ErrorAction SilentlyContinue
}

function create-output-dir
{
	mkdir $output_root -ErrorAction SilentlyContinue | out-null
}

#################### EOF ####################