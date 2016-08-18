import-module .\commandline.psm1 -force

Describe "string-param" {

	It "creates array param w/ format" {
		(string-param "param" "value" -format "/{0}: ") | Should Be @("/param:", "value")
	}

	It "creates array param w/o format" {
		(string-param "param" "value") | Should Be @("-param", "value")
	}

	It "creates array string w/ format" {
		(string-param "param" "value" -format "/formatted-{0}:") | Should Be @("/formatted-param:value")
	}
}

Describe "switch-param" {

	It "creates param w/ format when false" {
		(switch-param "param" $false -format "/{0}: ") | Should Be $null
	}

	It "creates param w/ format when true" {
		(switch-param "param" $true -format "/{0}: ") | Should Be @("/param:")
	}

	It "creates param w/ format when truthy" {
		(switch-param "param" "value" -format "-{0}") | Should Be @("-param")
	}
}

Describe "list-param" {

	It "creates param w/ format for separate items" {
		(list-param "param" @(1, 2, 3) -format "/{0}: ") | Should Be @("/param:", 1, "/param:", 2, "/param:", 3)
	}

	It "creates param w/o format for separate items" {
		(list-param "param" @(1, 2, 3)) | Should Be @("-param", 1, "-param:", 2, "-param", 3)
	}

	It "creates param w/ format for merged items" {
		(list-param "param" @(1, 2, 3) -format "/{0}:") | Should Be @("/param:1", "/param:2", "/param:3")
	}

	It "creates param w/ format for separate items" {
		(list-param "param" @(1, 2, 3) -join "/" -format "/{0}: ") | Should Be @("/param:", "1/2/3")
	}

	It "creates param w/ format for merged items" {
		(list-param "param" @(1, 2, 3) -join "/" -format "/{0}:") | Should Be @("/param:1/2/3")
	}
}

Describe "hash-param" {

	It "creates multi param w/ format for separate items" {
		$expected = hash-param "param" -hash @{a=1; b=2; c=3;} -format "/{0}: "
		$expected | Should Be @("/param:", "a=1", "/param:", "b=2", "/param:", "c=3")
	}

	It "creates multi param w/o format for separate items" {
		$expected = hash-param "param" -hash ([ordered] @{a=1; b=2; c=3;})
		$expected | Should Be @("-param", "a=1", "-param", "b=2", "-param", "c=3")
	}

	It "creates multi param w/ format for merged items" {
		$expected = hash-param "param" -hash ([ordered] @{a=1; b=2; c=3;}) -format "/{0}:"
		$expected | Should Be @("/param:a=1", "/param:b=2", "/param:c=3")
	}

	It "creates single param w/ format for separate items" {
		(hash-param "param" ([ordered] @{a=1; b=2; c=3;}) -join "/" -format "/{0}: ") | Should Be @("/param:", "a=1/b=2/c=3")
	}

	It "creates single param w/ format for merged items" {
		(hash-param "param" ([ordered] @{a=1; b=2; c=3;}) -join "/" -format "/{0}:") | Should Be @("/param:a=1/b=2/c=3")
	}
}
