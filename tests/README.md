# Build and test

[Back to the library README](../README.md).

Run from the repository root in PowerShell. All build outputs, including external
project outputs, stay in the selected artifacts folder.

```powershell
$artifacts = Join-Path $PWD '.artifacts/check-x64'
dotnet build tests/MouseGetWhichMonitor.Tests/MouseGetWhichMonitor.Tests.csproj -c Release -p:PlatformTarget=x64 --artifacts-path $artifacts
& 'C:/Program Files/dotnet/dotnet.exe' "$artifacts/bin/MouseGetWhichMonitor.Tests/release/MouseGetWhichMonitor.Tests.dll"
pwsh -File tests/Test-VersionReference.ps1
```

For x86, use `.artifacts/check-x86`, `-p:PlatformTarget=x86`, and
`C:/Program Files (x86)/dotnet/dotnet.exe`. Tests cover negative and fractional
coordinates, edges, overlaps, snapshot order, null inputs, cursor failure, and
read-only native calls. Live cursor parity is skipped if the sample is unstable.
NativeAOT publication and signing are handled separately.
