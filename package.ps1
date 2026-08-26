$ErrorActionPreference='Stop'
& "$PSScriptRoot\build.ps1"
$zip="$PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-win-x64.zip"
if(Test-Path $zip){Remove-Item -LiteralPath $zip}
Compress-Archive -Path "$PSScriptRoot\artifacts\publish\*" -DestinationPath $zip
Write-Host "Pacchetto standalone: $zip"
$wix=Get-Command wix -ErrorAction SilentlyContinue
if($wix){
  wix build "$PSScriptRoot\installer\Package.wxs" -d "PublishDir=$PSScriptRoot\artifacts\publish" -d "IconPath=$PSScriptRoot\Assets\WolfRadio.ico" -arch x64 -o "$PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-Setup.msi"
  Write-Host "Installer: $PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-Setup.msi"
} else {
  Write-Warning "WiX non trovato: ZIP creato, MSI ignorato. Installare WiX Toolset per compilare l'installer."
}
