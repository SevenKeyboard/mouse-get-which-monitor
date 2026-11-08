$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifacts = Join-Path $repoRoot ('.artifacts/version-check-' + [guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($artifacts) | Out-Null
$project = Join-Path $repoRoot 'src/SevenKeyboard.MouseGetWhichMonitor/SevenKeyboard.MouseGetWhichMonitor.csproj'
foreach ($testCase in @(
    @{ Version = '2.0.0'; Expected = '' },
    @{ Version = '2.1.0'; Expected = '' },
    @{ Version = '1.9.9'; Expected = 'SKLIB002' },
    @{ Version = '3.0.0'; Expected = 'SKLIB002' },
    @{ Version = ''; Expected = 'SKLIB001' }
)) {
    $fixture = Join-Path $artifacts ('fixture-' + [guid]::NewGuid().ToString('N') + '.csproj')
    $content = if ($testCase.Version) {
        '<Project><PropertyGroup><Version>' + $testCase.Version + '</Version></PropertyGroup></Project>'
    } else {
        '<Project />'
    }
    [System.IO.File]::WriteAllText($fixture, $content, [System.Text.UTF8Encoding]::new($false))
    $output = & dotnet msbuild $project -nologo -t:CheckLibraryVersions "-p:MonitorExGetUtilsProjectPath=$fixture" "-p:ArtifactsPath=$artifacts" -p:UseArtifactsOutput=true 2>&1
    $exitCode = $LASTEXITCODE
    if ($testCase.Expected -eq '') {
        if ($exitCode -ne 0) { throw "Expected success for $($testCase.Version): $output" }
    } elseif ($exitCode -eq 0 -or "$output" -notmatch $testCase.Expected) {
        throw "Expected $($testCase.Expected) for $($testCase.Version): $output"
    }
}
Write-Output 'PASS: 5 external ProjectReference/version-check cases.'
