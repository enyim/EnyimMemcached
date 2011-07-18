function global:get-assembly-title
{
	param([string] $Path)

	$file = get-item $Path
	$content = [io.file]::ReadAllBytes($file.fullname)
	$a = [System.Reflection.Assembly]::Load($content)
	$d = [System.Attribute]::GetCustomAttribute($a, [System.Reflection.AssemblyTitleAttribute])

	return $d.Title
}

function global:get-assembly-version
{
	param([string] $Path)

	$file = get-item $Path
	$content = [io.file]::ReadAllBytes($file.fullname)
	$a = [System.Reflection.Assembly]::Load($content)
	$name = $a.GetName()

	return $name.Version
}

$Markdown = $null

function global:transform-markdown
{
	param($TemplatePath, $FilePath, $Title)

	if ($Markdown -eq $null) {
		[System.Reflection.Assembly]::Load([io.file]::ReadAllBytes("$variable:build_root\markdownsharp.dll")) > $nul
		$Markdown = new-object MarkdownSharp.Markdown
	}	

	return (get-content $TemplatePath) -replace '\$title', $Title -replace '\$content', $Markdown.Transform([io.file]::ReadAllText($FilePath))
}

function global:get7zip
{
	$pfx86 = "${Env:ProgramFiles(x86)}"
	$pf = $env:ProgramFiles

	$tmp = join-path $pf "7-Zip\7z.exe"
	if (test-path $tmp) { return $tmp }

	$tmp = join-path $pfx86 "7-Zip\7z.exe"
	if (test-path $tmp) { return $tmp }

	# if this is an x86 host on x64 we'll get the x86 PF folder both time so try to get the x64 dir
	$tmp = join-path ($pf -replace ' \(x86\)$', "") "7-Zip\7z.exe"
	if (test-path $tmp) { return $tmp }

	throw "7-Zip is not installed."
}

# $zip = "$Env:ProgramFiles\7-Zip\7z.exe"
