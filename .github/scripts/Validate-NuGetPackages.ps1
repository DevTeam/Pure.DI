param(
    [Parameter(Mandatory)][string] $Directory,
    [Parameter(Mandatory)][string] $Version
)

$ErrorActionPreference = 'Stop'
$ids = @('Pure.DI', 'Pure.DI.MS', 'Pure.DI.Abstractions', 'Pure.DI.Agent', 'Pure.DI.Templates')
$files = @(Get-ChildItem -LiteralPath $Directory -Filter '*.nupkg' -File)
if ($files.Count -ne $ids.Count) { throw "Expected exactly five release packages, found $($files.Count)." }

foreach ($id in $ids) {
    $file = Join-Path $Directory "$id.$Version.nupkg"
    $archive = [System.IO.Compression.ZipFile]::OpenRead($file)
    try {
        $entries = @($archive.Entries | Where-Object { $_.FullName -like '*.nuspec' })
        if ($entries.Count -ne 1) { throw "Expected one nuspec in $file." }
        $reader = [System.IO.StreamReader]::new($entries[0].Open())
        try { [xml] $nuspec = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($nuspec.package.metadata.id -cne $id -or $nuspec.package.metadata.version -cne $Version) {
            throw "Unexpected package identity in $file."
        }
    } finally { $archive.Dispose() }
    $hash = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash
    $line = "$id $Version SHA256: $hash"
    Write-Host $line
    if ($env:GITHUB_STEP_SUMMARY) { $line >> $env:GITHUB_STEP_SUMMARY }
}
