$ErrorActionPreference = "Stop"
. $PSScriptRoot/Functions.ps1

$branchName = "upm"
$packageDirectory = Resolve-Path "Packages/com.quickeye.icon-browser"
$semVersion = Get-PackageVersion $packageDirectory

Write-Host @"
Update Branch
Name: $branchName
PackageDir: $packageDirectory
Ver: $semVersion
"@
Update-UpmBranch -BranchName $branchName -PackageVersion $semVersion -PackageDirectory $packageDirectory

