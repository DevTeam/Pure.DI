param([Parameter(Mandatory = $true)][string]$ToolDll)

$ErrorActionPreference = 'Stop'
$tool = (Resolve-Path $ToolDll).Path
$root = Join-Path ([IO.Path]::GetTempPath()) ('puredi-agent-smoke-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null

function Invoke-Agent([string[]]$Arguments, [int]$ExpectedExitCode = 0) {
    & dotnet $tool @Arguments --root $root
    if ($LASTEXITCODE -ne $ExpectedExitCode) {
        throw "Agent command returned $LASTEXITCODE; expected $ExpectedExitCode"
    }
}

$project = Join-Path $root 'Consumer.csproj'
$agents = Join-Path $root 'AGENTS.md'
$claude = Join-Path $root 'CLAUDE.md'
$skill = Join-Path $root '.agents/skills/pure-di/SKILL.md'

'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Pure.DI" Version="2.5.4" /></ItemGroup></Project>' | Set-Content $project
'# Team rules' | Set-Content $agents
'# Claude rules' | Set-Content $claude

Invoke-Agent @('init', '--dry-run')
if (Test-Path $skill) { throw 'Dry run wrote a skill' }
if ((Get-Content $agents -Raw) -match 'Pure.DI agent instructions') { throw 'Dry run changed AGENTS.md' }

Invoke-Agent @('init')
if (!(Test-Path $skill) -or !(Test-Path (Join-Path $root '.claude/skills/pure-di/SKILL.md'))) { throw 'A skill was not installed' }
if ((Get-Content $agents -Raw) -notmatch '# Team rules') { throw 'Existing AGENTS.md content was lost' }
if ((Get-Content $claude -Raw) -notmatch '# Claude rules') { throw 'Existing CLAUDE.md content was lost' }

$firstAgents = Get-Content $agents -Raw
Invoke-Agent @('init')
if ((Get-Content $agents -Raw) -ne $firstAgents) { throw 'Repeated init changed AGENTS.md' }

$originalSkill = Get-Content $skill -Raw
Add-Content $skill 'Local modification'
Invoke-Agent @('init') 1
if ((Get-Content $agents -Raw) -ne $firstAgents) { throw 'Failed init changed AGENTS.md' }
Set-Content $skill $originalSkill -NoNewline

Invoke-Agent @('remove')
if (Test-Path $skill) { throw 'Remove kept an unmodified installed skill' }
if ((Get-Content $agents -Raw).Trim() -ne '# Team rules') { throw 'Remove did not restore AGENTS.md' }
if ((Get-Content $claude -Raw).Trim() -ne '# Claude rules') { throw 'Remove did not restore CLAUDE.md' }

# A project created by dotnet new has guidance but no installer manifest.
$root = Join-Path ([IO.Path]::GetTempPath()) ('puredi-template-guidance-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$context = (Resolve-Path (Join-Path $PSScriptRoot '../agent-context')).Path
Copy-Item (Join-Path $context 'AGENTS.md') (Join-Path $root 'AGENTS.md')
Copy-Item (Join-Path $context 'CLAUDE.md') (Join-Path $root 'CLAUDE.md')
foreach ($agent in @('.agents', '.claude')) {
    $destination = Join-Path $root "$agent/skills/pure-di"
    New-Item -ItemType Directory -Path $destination -Force | Out-Null
    Copy-Item (Join-Path $context 'skills/pure-di/*') $destination -Recurse
}
Invoke-Agent @('remove')
if (Test-Path (Join-Path $root '.agents/skills/pure-di/SKILL.md')) { throw 'Remove kept a template skill' }
if (Test-Path (Join-Path $root 'AGENTS.md')) { throw 'Remove kept template-only instructions' }

Write-Output 'Pure.DI.Agent smoke test passed.'
