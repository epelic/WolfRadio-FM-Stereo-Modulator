$ErrorActionPreference='Stop'
$root=$PSScriptRoot
dotnet publish "$root\FmStereoModulator.csproj" -c Release -r win-x64 --self-contained true -o "$root\artifacts\publish"
Write-Host "Creato: $root\artifacts\publish\WolfRadio.exe"
