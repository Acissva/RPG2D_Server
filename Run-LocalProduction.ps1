param(
    [string]$PublishPath
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($PublishPath)) {
    $PublishPath = Join-Path $PSScriptRoot 'bin\publish\v0.0.18'
}
$publishDirectory = (Resolve-Path -LiteralPath $PublishPath).Path
$applicationDll = Join-Path $publishDirectory 'RPG2D_Server.dll'

if (-not (Test-Path -LiteralPath $applicationDll -PathType Leaf)) {
    throw 'RPG2D_Server.dll is missing. Run dotnet publish first.'
}

$previousDirectory = Get-Location
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__Rpg2D', 'Process')
$previousRedisConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__Redis', 'Process')
$previousRedisEnabled = [Environment]::GetEnvironmentVariable('Redis__Enabled', 'Process')
$passwordInput = $null
$connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()

try {
    $connectionBuilder.set_ConnectionString('Server=127.0.0.1;Port=3306;Database=rpg2d_dev;User ID=rpg2d_app;SslMode=Required;Pooling=True;Connection Timeout=2;Default Command Timeout=2;Cancellation Timeout=1;UseAffectedRows=False;')
    $passwordInput = Read-Host 'Local rpg2d_app password (hidden input)' -AsSecureString
    $connectionBuilder.Add('Password', ([System.Net.NetworkCredential]::new('', $passwordInput)).Password)
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Rpg2D', $connectionBuilder.get_ConnectionString(), 'Process')
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Redis', '127.0.0.1:6379,defaultDatabase=0', 'Process')
    [Environment]::SetEnvironmentVariable('Redis__Enabled', 'true', 'Process')
    Set-Location -LiteralPath $publishDirectory
    & dotnet $applicationDll --environment Production --urls http://127.0.0.1:5080 --DevelopmentNoticeApi:Enabled true
} finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Rpg2D', $previousConnection, 'Process')
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Redis', $previousRedisConnection, 'Process')
    [Environment]::SetEnvironmentVariable('Redis__Enabled', $previousRedisEnabled, 'Process')
    Set-Location -LiteralPath $previousDirectory.Path
    $connectionBuilder.Clear()

    if ($null -ne $passwordInput) {
        $passwordInput.Dispose()
    }

    Remove-Variable previousConnection, previousRedisConnection, previousRedisEnabled, passwordInput, connectionBuilder -ErrorAction SilentlyContinue
}
