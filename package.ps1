$ErrorActionPreference='Stop'
& "$PSScriptRoot\build.ps1"
$zip="$PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-win-x64.zip"
if(Test-Path $zip){Remove-Item -LiteralPath $zip}
Compress-Archive -Path "$PSScriptRoot\artifacts\publish\*" -DestinationPath $zip
Write-Host "Standalone package: $zip"
$wix=Get-Command wix -ErrorAction SilentlyContinue
if($wix){
  wix build "$PSScriptRoot\installer\Package.wxs" -ext WixToolset.UI.wixext -d "PublishDir=$PSScriptRoot\artifacts\publish" -d "IconPath=$PSScriptRoot\Assets\WolfRadio.ico" -d "LicensePath=$PSScriptRoot\EULA.rtf" -arch x64 -o "$PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-Setup.msi"
  Write-Host "Installer: $PSScriptRoot\artifacts\WolfRadio-FM-Stereo-Modulator-Setup.msi"
} else {
  Write-Warning "WiX was not found: the ZIP was created but the MSI was skipped. Install WiX Toolset to build the installer."
}
