param(
    [string]$PublishPath = (Join-Path $PSScriptRoot 'bin\publish\v0.0.17')
)

$ErrorActionPreference = 'Stop'
$publishDirectory = (Resolve-Path -LiteralPath $PublishPath).Path
$applicationDll = Join-Path $publishDirectory 'RPG2D_Server.dll'

if (-not (Test-Path -LiteralPath $applicationDll -PathType Leaf)) {
    throw 'RPG2D_Server.dll is missing. Run dotnet publish first.'
}

$previousDirectory = Get-Location
$previousConnection = [Environment]::GetEnvironmentVariable('ConnectionStrings__Rpg2D', 'Process')
$passwordInput = $null
$connectionBuilder = [System.Data.Common.DbConnectionStringBuilder]::new()

try {
    $connectionBuilder.set_ConnectionString('Server=127.0.0.1;Port=3306;Database=rpg2d_dev;User ID=rpg2d_app;SslMode=Required;Pooling=True;Connection Timeout=2;Default Command Timeout=2;Cancellation Timeout=1;UseAffectedRows=False;')
    $passwordInput = Read-Host 'Local rpg2d_app password (hidden input)' -AsSecureString
    $connectionBuilder.Add('Password', ([System.Net.NetworkCredential]::new('', $passwordInput)).Password)
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Rpg2D', $connectionBuilder.get_ConnectionString(), 'Process')
    Set-Location -LiteralPath $publishDirectory
    & dotnet $applicationDll --environment Production --urls http://127.0.0.1:5080 --DevelopmentNoticeApi:Enabled true
} finally {
    [Environment]::SetEnvironmentVariable('ConnectionStrings__Rpg2D', $previousConnection, 'Process')
    Set-Location -LiteralPath $previousDirectory.Path
    $connectionBuilder.Clear()

    if ($null -ne $passwordInput) {
        $passwordInput.Dispose()
    }

    Remove-Variable previousConnection, passwordInput, connectionBuilder -ErrorAction SilentlyContinue
}
