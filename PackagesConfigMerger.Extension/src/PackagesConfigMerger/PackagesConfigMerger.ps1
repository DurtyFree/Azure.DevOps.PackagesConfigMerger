[CmdletBinding()]
param(
    [string]$RootDirectory,
    [string]$NewPackagesConfigFilePath
)

begin
{
    $PackagesConfigMergerExe = ".\exe\PackagesConfigMerger.exe"
}

process
{
    Write-Verbose "Executing command: $RootDirectory $NewPackagesConfigFilePath"
    & $PackagesConfigMergerExe $RootDirectory $NewPackagesConfigFilePath
}

end
{

}