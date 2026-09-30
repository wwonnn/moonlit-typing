param([Parameter(Mandatory=$true)][string]$SourceDirectory)
$ErrorActionPreference = 'Stop'
$projectDirectory = Split-Path $PSScriptRoot -Parent
$sourceModels = Join-Path $SourceDirectory 'Models'
$sourceTextures = Join-Path $SourceDirectory 'Textures'
if (!(Test-Path -LiteralPath (Join-Path $sourceModels 'Scholar-Writing.fbx'))) { throw '승인된 Scholar-Writing.fbx가 없습니다.' }
if (!(Test-Path -LiteralPath (Join-Path $sourceTextures 'BaseColor.png'))) { throw '승인된 BaseColor.png가 없습니다.' }
$destination = Join-Path $projectDirectory 'Assets\Scholar'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
Copy-Item -LiteralPath $sourceModels -Destination $destination -Recurse -Force
Copy-Item -LiteralPath $sourceTextures -Destination $destination -Recurse -Force
Write-Output '승인 캐릭터 에셋을 가져왔습니다. Unity에서 BuildMoonlit.Prepare를 실행하세요.'
